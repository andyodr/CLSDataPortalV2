using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.Targets;

[Route("api/targets/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = new();

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(TargetGetAllObject value) {
		MeasureDataIndexListObject returnObject = new();
		List<MeasureDataReturnObject> measureDataList = new();
		List<long> id = new();

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.target, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var defs = from measuredef in _context.MeasureDefinition
					   where measuredef.MeasureType!.Id == value.measureTypeId
					   orderby measuredef.FieldNumber ascending, measuredef.Name
					   select measuredef;

			var msr = from m in _context.Measure
					  from t in m.Targets!
					  where t.Active == true && m.Hierarchy!.Id == value.hierarchyId
					  select t;

			foreach (var def in defs.Include(d => d.Unit)) {
				foreach (var t in msr.Include(t => t.User)) {
					if (def.Id == t.Measure!.MeasureDefinitionId) {
						MeasureDataReturnObject mdr = new() {
							id = t.Measure.Id,
							name = def.Name,
							target = t.Value,
							yellow = t.YellowValue,
							targetCount = t.Measure.Targets!.Count,
							expression = def.Expression,
							description = def.Description,
							units = def.Unit!.Short,
							targetId = t.Id,
							updated = t.UserId switch {
								null => Helper.LastUpdatedOnObj(t.LastUpdatedOn, Resource.SYSTEM),
								_ => Helper.LastUpdatedOnObj(t.LastUpdatedOn, t.User?.UserName)
							}
						};

						if (t.Value != null) {
							mdr.target = Math.Round((double)t.Value, def.Precision, MidpointRounding.AwayFromZero);
						}

						if (t.YellowValue != null) {
							mdr.yellow = Math.Round((double)t.YellowValue, def.Precision, MidpointRounding.AwayFromZero);
						}

						measureDataList.Add(mdr);
					}
				}
			}

			returnObject.confirmed = _config.usesSpecialHieararhies;

			returnObject.allow = false;
			if (_user.userRoleId == (int)Helper.userRoles.systemAdministrator)
				returnObject.allow = _user.hierarchyIds.Contains(value.hierarchyId);


			returnObject.data = measureDataList;

			_user.savedFilters[Helper.pages.target].hierarchyId = value.hierarchyId;
			_user.savedFilters[Helper.pages.target].measureTypeId = value.measureTypeId;
			returnObject.filter = _user.savedFilters[Helper.pages.target];
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] TargetGetAllObject value) {
		MeasureDataIndexListObject returnObject = new();
		List<MeasureDataReturnObject> measureDataList = new();
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
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
			if (value.target != null) {
				if (measureDef!.UnitId == 1 && (value.target < 0d || value.target > 1d)) {
					throw new Exception(Resource.VAL_VALUE_UNIT);
				}

				targetValue = Math.Round((double)value.target, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			if (value.yellow != null) {
				if (measureDef!.UnitId == 1 && (value.yellow < 0d || value.yellow > 1d)) {
					throw new Exception(Resource.VAL_VALUE_UNIT);
				}

				targetYellow = Math.Round((double)value.yellow, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			// Set current target to inactive if there are multiple targets
			long returnTargetId = target.Id;

			target.IsProcessed = (byte)Helper.IsProcessed.complete;
			target.LastUpdatedOn = lastUpdatedOn;
			target.Active = targetCount == 1 && target.Value == null && target.YellowValue == null;

			// Create new target and save if there are multiple targets for the same measure  
			if ((targetCount > 1 || !target.Active) && value.measureId != null) {
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

				Helper.addAuditTrail(
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
				target = value.target,
				yellow = value.yellow,
				targetId = returnTargetId,
				targetCount = _context.Target.Where(t => t.Measure!.Id == value.measureId).Count(),
				updated = Helper.LastUpdatedOnObj(lastUpdatedOn, _user.userName)
			});

			returnObject.data = measureDataList;
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut("{id}")]
	public ActionResult<JsonResult> applytochildren([FromBody] TargetGetAllObject value) {
		MeasureDataIndexListObject returnObject = new();
		var lastUpdatedOn = DateTime.Now;

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			List<int> hierarchyIds = new();
			hierarchyIds = GetAllChildren(value.hierarchyId);

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

					if (targetCount == 1 && target.Value == null && target.YellowValue == null) {
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
						Helper.addAuditTrail(
						  Resource.WEB_PAGES,
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

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
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
		if (confirmIntervals != null) {
			// Find current calendar Ids from confirmIntervals.
			if (confirmIntervals.weekly ?? false) {
				int cWeekly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.weekly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.Calendar!.Id == cWeekly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.monthly ?? false) {
				int cMonthly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.monthly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.Calendar!.Id == cMonthly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.quarterly ?? false) {
				int cQuarterly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.quarterly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.Calendar!.Id == cQuarterly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.yearly ?? false) {
				int cYearly = _context.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _context.MeasureData.Where(m => m.Measure!.Id == measureId && m.Calendar!.Id == cYearly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)Helper.IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}
		}
	}

	// DELETE api/values/5
	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
