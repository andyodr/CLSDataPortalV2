using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	/// <summary>
	/// Get Measure data
	/// </summary>
	[HttpGet]
	public ActionResult<MeasureDataIndexListObject> Get(int hierarchyId, int measureTypeId) {
		var returnObject = new MeasureDataIndexListObject { data = new() };

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var defs = from mdef in _context.MeasureDefinition
					   where mdef.MeasureTypeId == measureTypeId
					   orderby mdef.FieldNumber, mdef.Name
					   select mdef;

			var targets = from m in _context.Measure
						  from t in m.Targets!
						  where t.Active == true && m.Hierarchy!.Id == hierarchyId
						  select t;

			foreach (var def in defs.Include(d => d.Unit).AsNoTrackingWithIdentityResolution()) {
				foreach (var t in targets.Include(t => t.Measure).Include(t => t.User).AsNoTrackingWithIdentityResolution()) {
					if (def.Id == t.Measure!.MeasureDefinitionId) {
						var mdr = new MeasureDataReturnObject {
							Id = t.Measure.Id,
							Name = def.Name,
							Target = t.Value,
							Yellow = t.YellowValue,
							TargetCount = t.Measure.Targets!.Count,
							Expression = def.Expression,
							Description = def.Description,
							Units = def.Unit.Short,
							TargetId = t.Id,
							Updated = t.UserId switch {
								null => Helper.LastUpdatedOnObj(t.LastUpdatedOn, Resource.SYSTEM),
								_ => Helper.LastUpdatedOnObj(t.LastUpdatedOn, t.User?.UserName)
							}
						};

						if (t.Value is double v) {
							mdr.Target = Math.Round(v, def.Precision, MidpointRounding.AwayFromZero);
						}

						if (t.YellowValue is double y) {
							mdr.Yellow = Math.Round(y, def.Precision, MidpointRounding.AwayFromZero);
						}

						returnObject.data.Add(mdr);
					}
				}
			}

			returnObject.confirmed = _config.usesSpecialHieararhies;

			returnObject.allow = false;
			if (_user.userRoleId == (int)Helper.userRoles.systemAdministrator)
				returnObject.allow = _user.hierarchyIds.Contains(hierarchyId);

			_user.savedFilters[Helper.pages.target].hierarchyId = hierarchyId;
			_user.savedFilters[Helper.pages.target].measureTypeId = measureTypeId;
			returnObject.filter = _user.savedFilters[Helper.pages.target];
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexListObject> Put(TargetGetAllObject value) {
		var returnObject = new MeasureDataIndexListObject();
		List<MeasureDataReturnObject> measureDataList = new();
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var lastUpdatedOn = DateTime.Now;
			int targetCount = _context.Target.Where(t => t.Measure!.Id == value.measureId).Count();
			var target = _context.Target
				.Where(t => t.Measure!.Id == value.measureId && t.Active == true)
				.Include(t => t.Measure)
				.ThenInclude(m => m!.MeasureDefinition)
				.First();

			double? targetValue = null, targetYellow = null;
			var measureDef = target.Measure!.MeasureDefinition;
			if (value.target is not null) {
				if (measureDef!.UnitId == 1 && (value.target < 0d || value.target > 1d)) {
					return BadRequest(Resource.VAL_VALUE_UNIT);
				}

				targetValue = Math.Round((double)value.target, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			if (value.yellow is not null) {
				if (measureDef!.UnitId == 1 && (value.yellow < 0d || value.yellow > 1d)) {
					return BadRequest(Resource.VAL_VALUE_UNIT);
				}

				targetYellow = Math.Round((double)value.yellow, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			// Set current target to inactive if there are multiple targets
			long returnTargetId = target.Id;

			target.IsProcessed = (byte)Helper.IsProcessed.complete;
			target.LastUpdatedOn = lastUpdatedOn;
			target.Active = targetCount == 1 && target.Value is null && target.YellowValue is null;

			// Create new target and save if there are multiple targets for the same measure  
			if ((targetCount > 1 || !target.Active) && value.measureId is not null) {
				// Create new target and save      
				var newTarget = _context.Target.Add(new() {
					Active = true,
					Value = targetValue,
					YellowValue = targetYellow,
					MeasureId = value.measureId ?? 0L,
					LastUpdatedOn = lastUpdatedOn,
					UserId = _user.userId,
					IsProcessed = (byte)Helper.IsProcessed.complete
				}).Entity;

				_context.SaveChanges();
				returnTargetId = newTarget.Id;

				// Update Target Id for all Measure Data records for current intervals
				if (value.isCurrentUpdate ?? false) {
					UpdateCurrentTargets(newTarget.Id, value.confirmIntervals, value.measureId ?? 0L, _user.userId, lastUpdatedOn);
				}

				Helper.AddAuditTrail(_context,
					Resource.WEB_PAGES,
					"WEB-02",
					Resource.TARGET,
					@"Updated / ID=" + newTarget.Id.ToString() +
							" / Value=" + newTarget.Value.ToString() +
							" / Yellow=" + newTarget.YellowValue.ToString(),
					lastUpdatedOn,
					_user.userId
				);
			}

			_context.SaveChanges();
			measureDataList.Add(new() {
				Target = value.target,
				Yellow = value.yellow,
				TargetId = returnTargetId,
				TargetCount = _context.Target.Where(t => t.Measure!.Id == value.measureId).Count(),
				Updated = Helper.LastUpdatedOnObj(lastUpdatedOn, _user.userName)
			});

			returnObject.data = measureDataList;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut("{id}")]
	public ActionResult<MeasureDataIndexListObject> ApplyToChildren(TargetGetAllObject value) {
		var returnObject = new MeasureDataIndexListObject();
		var lastUpdatedOn = DateTime.Now;

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var hierarchyIds = GetAllChildren(value.hierarchyId);

			// getAllChildren will add the parent and the children to the list
			if (hierarchyIds.Count > 1) {
				// Remove the parent. No needed anymore 
				hierarchyIds.RemoveAt(0);

				var measureDefs = from measureDef in _context.MeasureDefinition
								  where measureDef.MeasureType!.Id == value.measureTypeId
								  select new { id = measureDef.Id };

				var masterList = from h in hierarchyIds
								 join md in measureDefs
								 on new { a = 1 }
								 equals new { a = 1 }
								 select new { mdId = md.id, hId = h };

				foreach (var record in masterList) {
					// Get parent target
					var pMeasure = _context.Measure.Where(m => m.MeasureDefinition!.Id == record.mdId && m.Hierarchy!.Id == value.hierarchyId);
					if (!pMeasure.Any()) {
						continue;
					}

					// Get parent target
					var pMeasureId = pMeasure.First().Id;
					var pTarget = _context.Target.Where(t => t.Measure!.Id == pMeasureId && t.Active == true);
					if (!pTarget.Any()) {
						continue;
					}

					// Set old target to Inactive
					var measureId = _context.Measure.Where(m => m.MeasureDefinition!.Id == record.mdId && m.Hierarchy!.Id == record.hId).First().Id;
					int targetCount = _context.Target.Where(t => t.Measure!.Id == measureId).Count();
					var target = _context.Target.Where(t => t.Measure!.Id == measureId && t.Active == true).First();

					// Set current target to inactive if there are multiple targets
					target.IsProcessed = (byte)Helper.IsProcessed.complete;
					target.LastUpdatedOn = lastUpdatedOn;

					if (targetCount == 1 && target.Value is null && target.YellowValue is null) {
						target.Active = true;
						target.Value = pTarget.First().Value;
						target.YellowValue = pTarget.First().YellowValue;
						_context.Target.Update(target);
						_ = _context.SaveChanges();
					}
					else {
						target.Active = false;
						_context.Target.Update(target);

						// Create new target and save if there are multiple targets for the same measure      
						var newTarget = _context.Target.Add(new() {
							Active = true,
							Value = pTarget.First().Value,
							YellowValue = pTarget.First().YellowValue,
							MeasureId = measureId,
							LastUpdatedOn = lastUpdatedOn,
							UserId = _user.userId,
							IsProcessed = (byte)Helper.IsProcessed.complete
						}).Entity;

						// Update Target Id for all Measure Data records for current intervals
						if (value.isCurrentUpdate ?? false) {
							UpdateCurrentTargets(newTarget.Id, value.confirmIntervals, measureId, _user.userId, lastUpdatedOn);
						}

						_ = _context.SaveChanges();
						Helper.AddAuditTrail(
						  _context, Resource.WEB_PAGES,
						   "WEB-02",
						   Resource.TARGET,
						   @"Apply to Children / ID=" + newTarget.Id.ToString() +
								   " / Value=" + newTarget.Value.ToString() +
								   " / Yellow=" + newTarget.YellowValue.ToString(),
						   lastUpdatedOn,
						   _user.userId
						);
					}
				}
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	private List<int> GetAllChildren(int hierarchyId) {
		var children = from child in _context.Hierarchy
					   where child.HierarchyParentId == hierarchyId
					   select child;

		List<int> returnList = new() { hierarchyId };
		foreach (var child in children) {
			returnList.AddRange(GetAllChildren(child.Id));
		}

		return returnList;
	}

	private void UpdateCurrentTargets(long newTargetId, TargetConfirmInterval confirmIntervals, long measureId, int userId, DateTime lastUpdatedOn) {
		// Update Target Id for all Measure Data records for current intervals
		if (confirmIntervals is not null) {
			// Find current calendar Ids from confirmIntervals.
			if (confirmIntervals.weekly ?? false) {
				int cWeekly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.weekly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cWeekly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.monthly ?? false) {
				int cMonthly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.monthly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cMonthly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.quarterly ?? false) {
				int cQuarterly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.quarterly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cQuarterly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.yearly ?? false) {
				int cYearly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cYearly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}
		}
	}
}
