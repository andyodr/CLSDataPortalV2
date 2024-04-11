using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Targets;

[ApiController]
[Route("api/targets/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class IndexController : BaseController
{
	/// <summary>
	/// Get Measure data
	/// </summary>
	[HttpGet]
	public ActionResult<MeasureDataIndexListObject> Get(int hierarchyId, int measureTypeId) {
		var result = new MeasureDataIndexListObject { Data = [] };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var targets = from df in Dbc.MeasureDefinition
						  from m in df.Measures!
						  from t in m.Targets!
						  where t.Active == true && m.HierarchyId == hierarchyId && df.MeasureTypeId == measureTypeId
						  orderby df.FieldNumber, df.Name
						  select t;

			foreach (var t in targets
					.Include(t => t.User)
					.Include(t => t.Measure)
					.ThenInclude(m => m!.MeasureDefinition)
					.ThenInclude(df => df!.Unit).AsNoTrackingWithIdentityResolution()) {
				MeasureDataReturnObject mdr = new() {
					Id = t.MeasureId,
					Name = t.Measure!.MeasureDefinition!.Name,
					Target = t.Value,
					Yellow = t.YellowValue,
					TargetCount = t.Measure.Targets!.Count,
					Expression = t.Measure.MeasureDefinition.Expression,
					Description = t.Measure.MeasureDefinition.Description,
					Units = t.Measure.MeasureDefinition.Unit.Short,
					Calculated = (t.Measure.Expression ?? false) || (t.Measure.Active ?? false) && (t.Measure.Rollup ?? false),
					TargetId = t.Id,
					Updated = t.UserId switch {
						null => LastUpdatedOnObj(t.LastUpdatedOn, Resource.SYSTEM),
						_ => LastUpdatedOnObj(t.LastUpdatedOn, t.User?.UserName)
					}
				};

				if (t.Value is double v) {
					mdr.Target = Math.Round(v, t.Measure.MeasureDefinition.Precision, MidpointRounding.AwayFromZero);
				}

				if (t.YellowValue is double y) {
					mdr.Yellow = Math.Round(y, t.Measure.MeasureDefinition.Precision, MidpointRounding.AwayFromZero);
				}

				result.Data.Add(mdr);
			}

			result.Confirmed = Config.UsesSpecialHierarchies;
			result.Allow = User.IsInRole(Roles.SystemAdministrator.ToString()) && Dbc
				.UserHierarchy
				.Where(d => d.UserId == _user.Id && d.HierarchyId == hierarchyId).Any();
			_user.savedFilters[Pages.Target].hierarchyId = hierarchyId;
			_user.savedFilters[Pages.Target].measureTypeId = measureTypeId;
			result.Filter = _user.savedFilters[Pages.Target];
			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexListObject> Put(TargetGetAllObject body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var lastUpdatedOn = DateTime.Now;
			var m = Dbc.Measure.Where(m => m.Id == body.MeasureId)
				.Select(m => new {
					m.MeasureDefinitionId,
					m.MeasureDefinition!.Precision,
					m.HierarchyId
				}).Single();

			ImportTarget importTarget = new() {
				HierarchyId = m.HierarchyId,
				MeasureDefinitionId = m.MeasureDefinitionId,
				Target = body.Target,
				Yellow = body.Yellow,
				Precision = m.Precision
			};
			if (Dbc.ValidateTargetImport(importTarget, _user.Id) is ImportErrorResult err) {
				return BadRequest(err.Message);
			};
			var errs = Dbc.ImportTarget(importTarget, _user.Id, lastUpdatedOn);
			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-02",
				Resource.TARGET,
				$"Updated / ID={body.MeasureId} / Value={body.Target} / Yellow={body.Yellow}",
				lastUpdatedOn,
				_user.Id
			);

			var target = Dbc.Target.Where(t => t.MeasureId == body.MeasureId && t.Active == true).First();
			return new MeasureDataIndexListObject { Data = [new() {
				Target = body.Target,
				Yellow = body.Yellow,
				TargetId = target.Id,
				TargetCount = Dbc.Target.Where(t => t.MeasureId == body.MeasureId).Count(),
				Updated = LastUpdatedOnObj(lastUpdatedOn, _user.UserName)
			}]};
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	[HttpPut("[action]")]
	public ActionResult<MeasureDataIndexListObject> ApplyToChildren(TargetGetAllObject body) {
		var result = new MeasureDataIndexListObject();
		var lastUpdatedOn = DateTime.Now;
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var hierarchyIds = GetAllChildren(body.HierarchyId);

			// getAllChildren will add the parent and the children to the list
			if (hierarchyIds.Count > 1) {
				// Remove the parent. No needed anymore
				hierarchyIds.RemoveAt(0);

				var measureDefs = from measureDef in Dbc.MeasureDefinition
								  where measureDef.MeasureTypeId == body.MeasureTypeId
								  select new { id = measureDef.Id };

				var masterList = from h in hierarchyIds
								 join md in measureDefs
								 on new { a = 1 }
								 equals new { a = 1 }
								 select new { mdId = md.id, hId = h };

				foreach (var record in masterList) { // cross product of hierarchyIds and measureDefs matching measureTypeId
					// Get parent target
					var pMeasure = Dbc.Measure.Where(m => m.MeasureDefinitionId == record.mdId && m.HierarchyId == body.HierarchyId);
					if (!pMeasure.Any()) {
						continue;
					}

					// Get parent target
					var pMeasureId = pMeasure.First().Id;
					var pTarget = Dbc.Target.Where(t => t.MeasureId == pMeasureId && t.Active == true);
					if (!pTarget.Any()) {
						continue;
					}

					// Set old target to Inactive
					var measureId = Dbc.Measure.Where(m => m.MeasureDefinitionId == record.mdId && m.HierarchyId == record.hId).First().Id;
					int targetCount = Dbc.Target.Where(t => t.MeasureId == measureId).Count();
					var target = Dbc.Target.Where(t => t.MeasureId == measureId && t.Active == true).First();

					// Set current target to inactive if there are multiple targets
					target.IsProcessed = (byte)IsProcessed.Complete;
					target.LastUpdatedOn = lastUpdatedOn;

					if (targetCount == 1 && target.Value is null && target.YellowValue is null) {
						target.Active = true;
						target.Value = pTarget.First().Value;
						target.YellowValue = pTarget.First().YellowValue;
						_ = Dbc.SaveChanges();
					}
					else {
						target.Active = false;

						// Create new target and save if there are multiple targets for the same measure
						var newTarget = Dbc.Target.Add(new() {
							Active = true,
							Value = pTarget.First().Value,
							YellowValue = pTarget.First().YellowValue,
							MeasureId = measureId,
							LastUpdatedOn = lastUpdatedOn,
							UserId = _user.Id,
							IsProcessed = (byte)IsProcessed.Complete
						}).Entity;

						// Update Target Id for all Measure Data records for current intervals
						if (body.IsCurrentUpdate ?? false) {
							UpdateCurrentTargets(newTarget.Id, body.ConfirmIntervals, measureId, _user.Id, lastUpdatedOn);
						}

						_ = Dbc.SaveChanges();
						Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-02",
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
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	private List<int> GetAllChildren(int hierarchyId) {
		return Dbc.Hierarchy.FromSql($"""
			WITH r AS
				(SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed
				FROM Hierarchy WHERE HierarchyParentId = {hierarchyId}
				UNION ALL
				SELECT ch.Id, ch.HierarchyLevelId, ch.HierarchyParentId, ch.[Name], ch.Active, ch.LastUpdatedOn, ch.IsProcessed
				FROM Hierarchy ch JOIN r ON ch.HierarchyParentId = r.Id
				WHERE ch.Active = 1)
			SELECT Id, HierarchyLevelId, HierarchyParentId, [Name], Active, LastUpdatedOn, IsProcessed FROM r
			""").AsEnumerable().Select(h => h.Id).ToList();
	}

	private void UpdateCurrentTargets(long newTargetId, TargetConfirmInterval confirmIntervals, long measureId, int userId, DateTime lastUpdatedOn) {
		// Update Target Id for all Measure Data records for current intervals
		if (confirmIntervals is not null) {
			// Find current calendar Ids from confirmIntervals.
			if (confirmIntervals.Weekly ?? false) {
				int cWeekly = Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Weekly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = Dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cWeekly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.Complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.Monthly ?? false) {
				int cMonthly = Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Monthly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = Dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cMonthly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.Complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.Quarterly ?? false) {
				int cQuarterly = Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Quarterly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = Dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cQuarterly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.Complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}

			if (confirmIntervals.Yearly ?? false) {
				int cYearly = Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).First().Id;
				var measureData = Dbc.MeasureData.Where(m => m.Measure!.Id == measureId && m.CalendarId == cYearly);
				foreach (var item in measureData) {
					item.TargetId = newTargetId;
					item.IsProcessed = (byte)IsProcessed.Complete;
					item.UserId = userId;
					item.LastUpdatedOn = lastUpdatedOn;
				}
			}
		}
	}
}
