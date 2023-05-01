using Deliver.WebApi.Data;
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
		var returnObject = new MeasureDefinitionIndexReturnObject {
			Units = new(),
			Intervals = new(),
			AggFunctions = AggregationFunctions.List,
			MeasureTypes = new()
		};

		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var intervals = Dbc.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals) {
				returnObject.Intervals.Add(new IntervalsObject { Id = interval.Id, Name = interval.Name });
			}

			var units = Dbc.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				returnObject.Units.Add(new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			var measureTypes = Dbc.MeasureType.OrderBy(m => m.Id);
			foreach (var mt in measureTypes) {
				returnObject.MeasureTypes.Add(new(mt.Id, mt.Name, mt.Description));
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	[HttpPost]
	public ActionResult<MeasureDefinitionIndexReturnObject> Post(MeasureDefinitionAdd body) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new MeasureDefinitionIndexReturnObject {
				Units = new List<UnitsObject>(),
				Intervals = new(),
				MeasureTypes = new(),
				Data = new()
			};

			// Validates name and variable name
			int validateCount = Dbc.MeasureDefinition
			  .Where(m =>
				m.Name.Trim().ToLower() == body.Name.Trim().ToLower() ||
				m.VariableName.Trim().ToLower() == body.VarName.Trim().ToLower()).Count();

			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}

			var intervals = Dbc.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals) {
				result.Intervals.Add(new() { Id = interval.Id, Name = interval.Name });
			}

			var units = Dbc.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				result.Units.Add(new() { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			var measureTypes = Dbc.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes) {
				result.MeasureTypes.Add(new(measureType.Id, measureType.Name, measureType.Description));
			}

			// Get Values from Page
			if (body.Expression is null) {
				body.Calculated = false;
			}
			else {
				body.Calculated = body.Expression.Trim().Length > 0;
				body.Expression = body.Expression.Replace(" \"", "\"").Replace("\" ", "\"");
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

			Dbc.SaveChanges();
			AddAuditTrail(Dbc,
				Resource.WEB_PAGES,
				"WEB-04",
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
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
