using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	/// <summary>
	/// Get Measure data
	/// </summary>
	[HttpGet]
	public ActionResult<MeasureDataIndexListObject> Get(int hierarchyId, int measureTypeId) {
		var result = new MeasureDataIndexListObject { Data = new List<MeasureDataReturnObject>() };

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var defs = from mdef in _dbc.MeasureDefinition
					   where mdef.MeasureTypeId == measureTypeId
					   orderby mdef.FieldNumber, mdef.Name
					   select mdef;

			var targets = from m in _dbc.Measure
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
								null => LastUpdatedOnObj(t.LastUpdatedOn, Resource.SYSTEM),
								_ => LastUpdatedOnObj(t.LastUpdatedOn, t.User?.UserName)
							}
						};

						if (t.Value is double v) {
							mdr.Target = Math.Round(v, def.Precision, MidpointRounding.AwayFromZero);
						}

						if (t.YellowValue is double y) {
							mdr.Yellow = Math.Round(y, def.Precision, MidpointRounding.AwayFromZero);
						}

						result.Data.Add(mdr);
					}
				}
			}

			result.Confirmed = _config.usesSpecialHieararhies;
			result.Allow = User.IsInRole(Roles.SystemAdministrator.ToString()) && _dbc
				.UserHierarchy
				.Where(d => d.UserId == _user.Id && d.HierarchyId == hierarchyId).Any();
			_user.savedFilters[pages.target].hierarchyId = hierarchyId;
			_user.savedFilters[pages.target].measureTypeId = measureTypeId;
			result.Filter = _user.savedFilters[pages.target];
			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexListObject> Put(TargetGetAllObject body) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var lastUpdatedOn = DateTime.Now;
			int targetCount = _dbc.Target.Where(t => t.Measure!.Id == body.MeasureId).Count();
			var target = _dbc.Target
				.Where(t => t.Measure!.Id == body.MeasureId && t.Active == true)
				.Include(t => t.Measure)
				.ThenInclude(m => m!.MeasureDefinition)
				.First();

			double? targetValue = null, targetYellow = null;
			var measureDef = target.Measure!.MeasureDefinition;
			if (body.Target is not null) {
				if (measureDef!.UnitId == 1 && (body.Target < 0d || body.Target > 1d)) {
					return BadRequest(Resource.VAL_VALUE_UNIT);
				}

				targetValue = Math.Round((double)body.Target, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			if (body.Yellow is not null) {
				if (measureDef!.UnitId == 1 && (body.Yellow < 0d || body.Yellow > 1d)) {
					return BadRequest(Resource.VAL_VALUE_UNIT);
				}

				targetYellow = Math.Round((double)body.Yellow, measureDef.Precision, MidpointRounding.AwayFromZero);
			}

			// Set current target to inactive if there are multiple targets
			long returnTargetId = target.Id;

			target.IsProcessed = (byte)IsProcessed.complete;
			target.LastUpdatedOn = lastUpdatedOn;
			target.Active = targetCount == 1 && target.Value is null && target.YellowValue is null;

			// Create new target and save if there are multiple targets for the same measure
			if ((targetCount > 1 || !target.Active) && body.MeasureId is not null) {
				// Create new target and save
				var newTarget = _dbc.Target.Add(new() {
					Active = true,
					Value = targetValue,
					YellowValue = targetYellow,
					MeasureId = body.MeasureId ?? 0L,
					LastUpdatedOn = lastUpdatedOn,
					UserId = _user.Id,
					IsProcessed = (byte)IsProcessed.complete
				}).Entity;

				_dbc.SaveChanges();
				returnTargetId = newTarget.Id;

				// Update Target Id for all Measure Data records for current intervals
				if (body.IsCurrentUpdate ?? false) {
					UpdateCurrentTargets(newTarget.Id, body.ConfirmIntervals, body.MeasureId ?? 0L, _user.Id, lastUpdatedOn);
				}

				AddAuditTrail(_dbc,
					Resource.WEB_PAGES,
					"WEB-02",
					Resource.TARGET,
					@"Updated / ID=" + newTarget.Id.ToString() +
							" / Value=" + newTarget.Value.ToString() +
							" / Yellow=" + newTarget.YellowValue.ToString(),
					lastUpdatedOn,
					_user.Id
				);
			}

			_dbc.SaveChanges();
			return new MeasureDataIndexListObject { Data = new MeasureDataReturnObject[] { new() {
				Target = body.Target,
				Yellow = body.Yellow,
				TargetId = returnTargetId,
				TargetCount = _dbc.Target.Where(t => t.Measure!.Id == body.MeasureId).Count(),
				Updated = LastUpdatedOnObj(lastUpdatedOn, _user.UserName)
			}}};
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut("[action]")]
	public ActionResult<MeasureDataIndexListObject> ApplyToChildren(TargetGetAllObject body) {
		var result = new MeasureDataIndexListObject();
		var lastUpdatedOn = DateTime.Now;

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var hierarchyIds = GetAllChildren(body.HierarchyId);

			// getAllChildren will add the parent and the children to the list
			if (hierarchyIds.Count > 1) {
				// Remove the parent. No needed anymore
				hierarchyIds.RemoveAt(0);

				var measureDefs = from measureDef in _dbc.MeasureDefinition
								  where measureDef.MeasureTypeId == body.MeasureTypeId
								  select new { id = measureDef.Id };

				var masterList = from h in hierarchyIds
								 join md in measureDefs
								 on new { a = 1 }
								 equals new { a = 1 }
								 select new { mdId = md.id, hId = h };

				foreach (var record in masterList) { // cross product of hierarchyIds and measureDefs matching measureTypeId
					// Get parent target
					var pMeasure = _dbc.Measure.Where(m => m.MeasureDefinitionId == record.mdId && m.HierarchyId == body.HierarchyId);
					if (!pMeasure.Any()) {
						continue;
					}

					// Get parent target
					var pMeasureId = pMeasure.First().Id;
					var pTarget = _dbc.Target.Where(t => t.MeasureId == pMeasureId && t.Active == true);
					if (!pTarget.Any()) {
						continue;
					}

					// Set old target to Inactive
					var measureId = _dbc.Measure.Where(m => m.MeasureDefinitionId == record.mdId && m.HierarchyId == record.hId).First().Id;
					int targetCount = _dbc.Target.Where(t => t.MeasureId == measureId).Count();
					var target = _dbc.Target.Where(t => t.MeasureId == measureId && t.Active == true).First();

					// Set current target to inactive if there are multiple targets
					target.IsProcessed = (byte)IsProcessed.complete;
					target.LastUpdatedOn = lastUpdatedOn;

					if (targetCount == 1 && target.Value is null && target.YellowValue is null) {
						target.Active = true;
						target.Value = pTarget.First().Value;
						target.YellowValue = pTarget.First().YellowValue;
						_ = _dbc.SaveChanges();
					}
					else {
						target.Active = false;

						// Create new target and save if there are multiple targets for the same measure
						var newTarget = _dbc.Target.Add(new() {
							Active = true,
							Value = pTarget.First().Value,
							YellowValue = pTarget.First().YellowValue,
							MeasureId = measureId,
							LastUpdatedOn = lastUpdatedOn,
							UserId = _user.Id,
							IsProcessed = (byte)IsProcessed.complete
						}).Entity;

						// Update Target Id for all Measure Data records for current intervals
						if (body.IsCurrentUpdate ?? false) {
							UpdateCurrentTargets(newTarget.Id, body.ConfirmIntervals, measureId, _user.Id, lastUpdatedOn);
						}

						_ = _dbc.SaveChanges();
						AddAuditTrail(
						  _dbc, Resource.WEB_PAGES,
						   "WEB-02",
						   Resource.TARGET,
						   @"Apply to Children / ID=" + newTarget.Id.ToString() +
								   " / Value=" + newTarget.Value.ToString() +
								   " / Yellow=" + newTarget.YellowValue.ToString(),
						   lastUpdatedOn,
						   _user.Id
						);
					}
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	private List<int> GetAllChildren(int hierarchyId) {
		return _dbc.Hierarchy.FromSql($@"WITH p AS
			(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
			FROM Hierarchy WHERE HierarchyParentId = {hierarchyId}
			UNION ALL
			SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
			FROM Hierarchy ch JOIN p ON ch.HierarchyParentId = p.Id
			WHERE ch.Active = 1)
			SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM p
			").AsEnumerable().Select(h => h.Id).ToList();
	}

	private void UpdateCurrentTargets(long newTargetId, TargetConfirmInterval confirmIntervals, long measureId, int userId, DateTime lastUpdatedOn) {
		// Update Target Id for all Measure Data records for current intervals
		if (confirmIntervals is not null) {
			// Find current calendar Ids from confirmIntervals.
			if (confirmIntervals.weekly ?? false) {
				int cWeekly = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Weekly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cWeekly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.monthly ?? false) {
				int cMonthly = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Monthly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cMonthly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.quarterly ?? false) {
				int cQuarterly = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Quarterly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cQuarterly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.yearly ?? false) {
				int cYearly = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = _dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cYearly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}
		}
	}
}
