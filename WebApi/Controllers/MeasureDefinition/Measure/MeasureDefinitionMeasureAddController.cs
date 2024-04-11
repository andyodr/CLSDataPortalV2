using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition.Measure;

[ApiController]
[Route("api/measureDefinition/measure/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class AddController : BaseController
{
	[HttpGet]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			return new MeasureDefinitionIndexReturnObject {
				Units = Dbc.Unit.OrderBy(u => u.Id)
					.Select(unit => new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short }).ToArray(),
				Intervals = Dbc.Interval.OrderBy(i => i.Id)
					.Select(item => new IntervalsObject { Id = item.Id, Name = item.Name }).ToArray(),
				AggFunctions = AggregationFunctions.List,
				MeasureTypes = Dbc.MeasureType.OrderBy(m => m.Id)
					.Select(mt => new Type.MeasureType(mt.Id, mt.Name, mt.Description)).ToArray()
			};
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	[HttpPost]
	public ActionResult<MeasureDefinitionIndexReturnObject> Post(MeasureDefinitionAdd body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new MeasureDefinitionIndexReturnObject { Data = new List<MeasureDefinitionEdit>() };

			// Validates name and variable name
			int invalidCount = Dbc.MeasureDefinition
			  .Where(m =>
				m.Name.Trim().ToLower() == body.Name.Trim().ToLower() ||
				m.VariableName.Trim().ToLower() == body.VarName.Trim().ToLower()).Count();

			if (invalidCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}

			result.Intervals = Dbc.Interval.OrderBy(i => i.Id)
				.Select(item => new IntervalsObject { Id = item.Id, Name = item.Name }).ToArray();
			result.Units = Dbc.Unit.OrderBy(u => u.Id)
				.Select(unit => new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short }).ToArray();
			result.MeasureTypes = Dbc.MeasureType.OrderBy(m => m.Id)
				.Select(mt => new Type.MeasureType(mt.Id, mt.Name, mt.Description)).ToArray();

			// Get Values from Page
			if (body.Expression is null) {
				body.Calculated = false;
			}
			else {
				body.Calculated = body.Expression.Trim().Length > 0;
				body.Expression = body.Expression.Replace(@" """, @"""").Replace(@""" ", @"""");
			}

			bool daily, weekly, monthly, quarterly, yearly = false;
			daily = (body.Daily ?? false) && body.IntervalId != (int)Intervals.Daily;
			weekly = (body.Weekly ?? false) && body.IntervalId != (int)Intervals.Weekly;
			monthly = (body.Monthly ?? false) && body.IntervalId != (int)Intervals.Monthly;
			quarterly = (body.Quarterly ?? false) && body.IntervalId != (int)Intervals.Quarterly;
			yearly = (body.Yearly ?? false) && body.IntervalId != (int)Intervals.Yearly;
			body.AggFunctionId ??= (byte)EnumAggFunctions.summation;
			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			var currentMD = new Data.Models.MeasureDefinition {
				Name = body.Name,
				MeasureTypeId = body.MeasureTypeId,
				VariableName = body.VarName,
				Description = body.Description,
				Precision = body.Precision,
				Priority = (short)body.Priority,
				FieldNumber = body.FieldNumber,
				UnitId = body.UnitId,
				Calculated = (bool)body.Calculated,
				Expression = body.Expression,
				ReportIntervalId = body.IntervalId,
				AggDaily = daily,
				AggWeekly = weekly,
				AggMonthly = monthly,
				AggQuarterly = quarterly,
				AggYearly = yearly
			};

			if (daily || weekly || monthly || quarterly || yearly) {
				currentMD.AggFunction = body.AggFunctionId;
				if (currentMD.Calculated != true && body.AggFunctionId == (byte)EnumAggFunctions.expression) {
					currentMD.AggFunction = (byte)EnumAggFunctions.summation;
				}
			}
			else {
				currentMD.AggFunction = null;
			}

			currentMD.LastUpdatedOn = lastUpdatedOn;
			currentMD.IsProcessed = (byte)IsProcessed.Complete;

			var test = Dbc.MeasureDefinition.Add(currentMD);
			Dbc.SaveChanges();
			MeasureDefinitionEdit md = new(body) { Id = currentMD.Id };
			result.Data.Add(md);

			// Create Measure and Target records
			string measuresAndTargets = CreateMeasuresAndTargets(Dbc, _user.Id, md);
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-04",
				Resource.MEASURE_DEFINITION,
				@"Added / ID=" + currentMD.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			// Create Measure Data records for current intervals
			CreateMeasureDataRecords(Dbc, body.IntervalId, currentMD.Id);

			if (weekly) {
				CreateMeasureDataRecords(Dbc, (int)Intervals.Weekly, currentMD.Id);
			}

			if (monthly) {
				CreateMeasureDataRecords(Dbc, (int)Intervals.Monthly, currentMD.Id);
			}

			if (quarterly) {
				CreateMeasureDataRecords(Dbc, (int)Intervals.Quarterly, currentMD.Id);
			}

			if (yearly) {
				CreateMeasureDataRecords(Dbc, (int)Intervals.Yearly, currentMD.Id);
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	internal static string CreateMeasuresAndTargets(ApplicationDbContext dbc, int userId, MeasureDefinitionEdit measureDef) {
		try {
			string result = string.Empty;
			var hierarchyRecords = from record in dbc.Hierarchy
								   select new { id = record.Id };
			var dtNow = DateTime.Now;
			foreach (var id in hierarchyRecords) {
				//create Measure records
				_ = dbc.Measure.Add(new() {
					HierarchyId = id.id,
					MeasureDefinitionId = measureDef.Id,
					Active = true,
					Expression = measureDef.Calculated,
					Rollup = true,
					LastUpdatedOn = dtNow
				});
			}

			dbc.SaveChanges();
			var measures = from measure in dbc.Measure
						   where measure.MeasureDefinitionId == measureDef.Id
						   select new { id = measure.Id };
			//make target ids
			foreach (var measure in measures) {
				_ = dbc.Target.Add(new() {
					MeasureId = measure.id,
					Active = true,
					UserId = userId,
					IsProcessed = (byte)IsProcessed.Complete,
					LastUpdatedOn = dtNow
				});
			}

			dbc.SaveChanges();
			return result;
		}
		catch (Exception e) {
			return e.Message;
		}
	}
}
