using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureDefinition.Measure;

[ApiController]
[Route("api/measureDefinition/measure/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public sealed class EditController : BaseController
{
	[HttpGet("{measureDefinitionId:min(1)}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get(int measureDefinitionId) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			Units = new(),
			Intervals = new(),
			MeasureTypes = new(),
			AggFunctions = AggregationFunctions.List,
			Data = new()
		};

		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var units = Dbc.Unit.OrderBy(u => u.Id);
			foreach (var unit in units.AsNoTracking()) {
				returnObject.Units.Add(new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			var intervals = Dbc.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals.AsNoTracking()) {
				returnObject.Intervals.Add(new IntervalsObject { Id = interval.Id, Name = interval.Name });
			}

			var measureTypes = Dbc.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTracking()) {
				returnObject.MeasureTypes
					.Add(new(measureType.Id, measureType.Name, measureType.Description));
			}

			foreach (var md in Dbc.MeasureDefinition.Where(md => md.Id == measureDefinitionId)) {
				MeasureDefinitionEdit currentMD = new() {
					Id = md.Id,
					MeasureTypeId = md.MeasureTypeId,
					Name = md.Name,
					VarName = md.VariableName,
					Description = md.Description,
					Expression = md.Expression,
					Precision = md.Precision,
					Priority = md.Priority,
					FieldNumber = md.FieldNumber,
					UnitId = md.UnitId,
					Calculated = md.Calculated,

					//find which interval Id to give
					//currentMD.intervalId = Helper.findIntervalId(currentMD, _intervalsRepository);
					IntervalId = md.ReportIntervalId
				};

				bool daily, weekly, monthly, quarterly, yearly = false;

				daily = (md.AggDaily ?? false) && currentMD.IntervalId != (int)Intervals.Daily;
				weekly = (md.AggWeekly ?? false) && currentMD.IntervalId != (int)Intervals.Weekly;
				monthly = (md.AggMonthly ?? false) && currentMD.IntervalId != (int)Intervals.Monthly;
				quarterly = (md.AggQuarterly ?? false) && currentMD.IntervalId != (int)Intervals.Quarterly;
				yearly = (md.AggYearly ?? false) && currentMD.IntervalId != (int)Intervals.Yearly;

				currentMD.Daily = daily;
				currentMD.Weekly = weekly;
				currentMD.Monthly = monthly;
				currentMD.Quarterly = quarterly;
				currentMD.Yearly = yearly;
				currentMD.AggFunctionId = md.AggFunction;
				returnObject.Data.Add(currentMD);
			}
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	[HttpPut("{id}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Put(int id, MeasureDefinitionEdit body) {
		var result = new MeasureDefinitionIndexReturnObject {
			Units = new(),
			Intervals = new(),
			MeasureTypes = new(),
			AggFunctions = AggregationFunctions.List,
			Data = new()
		};

		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var intervals = Dbc.Interval.OrderBy(i => i.Id);

			var units = Dbc.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				result.Units.Add(new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			foreach (var interval in intervals) {
				result.Intervals.Add(new IntervalsObject { Id = interval.Id, Name = interval.Name });
			}

			var measureTypes = Dbc.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes) {
				result.MeasureTypes.Add(new(measureType.Id, measureType.Name, measureType.Description));
			}

			var mDef = Dbc.MeasureDefinition.Where(m => m.Id == body.Id).First();

			// Validates name and variable name
			bool exists = Dbc.MeasureDefinition
				.Where(m =>
					m.Id != body.Id &&
					(m.Name.Trim().ToLower() == body.Name.Trim().ToLower() || m.VariableName.Trim().ToLower() == body.VarName.Trim().ToLower())
				).Any();

			if (exists) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}

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

			// Check if some values were changed in order to create new measure data records
			bool createMeasureData = mDef.ReportIntervalId != body.IntervalId ||
									 mDef.AggDaily != daily ||
									 mDef.AggWeekly != weekly ||
									 mDef.AggMonthly != monthly ||
									 mDef.AggQuarterly != quarterly ||
									 mDef.AggYearly != yearly;

			// Check if some values were changed in order to update IsProcessed to 1 for measure data records
			bool updateMeasureData = mDef.VariableName != body.VarName ||
									 mDef.Expression != body.Expression ||
									 mDef.AggFunction != body.AggFunctionId ||
									 createMeasureData;

			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			mDef.Id = body.Id;
			mDef.Name = body.Name;
			mDef.MeasureTypeId = body.MeasureTypeId;
			mDef.VariableName = body.VarName;
			mDef.Description = body.Description;
			mDef.Precision = body.Precision;
			mDef.Priority = (short)body.Priority;
			mDef.FieldNumber = body.FieldNumber;
			mDef.UnitId = body.UnitId;
			mDef.Calculated = (bool)body.Calculated;
			mDef.Expression = body.Expression;
			mDef.ReportIntervalId = body.IntervalId;
			mDef.AggDaily = daily;
			mDef.AggWeekly = weekly;
			mDef.AggMonthly = monthly;
			mDef.AggQuarterly = quarterly;
			mDef.AggYearly = yearly;

			if (daily || weekly || monthly || quarterly || yearly) {
				mDef.AggFunction = body.AggFunctionId;

				if (mDef.Calculated != true && body.AggFunctionId == (byte)EnumAggFunctions.expression) {
					mDef.AggFunction = (byte)EnumAggFunctions.summation;
				}
			}
			else {
				mDef.AggFunction = null;
			}

			mDef.LastUpdatedOn = lastUpdatedOn;
			mDef.IsProcessed = (byte)IsProcessed.Complete;

			Dbc.SaveChanges();
			result.Data.Add(body);

			// Update IsProcessed to 1 for Measure Data records
			if (updateMeasureData) {
				UpdateMeasureDataIsProcessed(Dbc, mDef.Id, _user.Id);
			}

			AddAuditTrail(Dbc,
				Resource.WEB_PAGES,
				"WEB-04",
				Resource.MEASURE_DEFINITION,
				@"Updated / ID=" + mDef.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			// Create Measure Data records for current intervals if they don't exist
			if (createMeasureData) {
				CreateMeasureDataRecords(Dbc, body.IntervalId, mDef.Id);
				if (weekly) {
					CreateMeasureDataRecords(Dbc, (int)Intervals.Weekly, mDef.Id);
				}

				if (monthly) {
					CreateMeasureDataRecords(Dbc, (int)Intervals.Monthly, mDef.Id);
				}

				if (quarterly) {
					CreateMeasureDataRecords(Dbc, (int)Intervals.Quarterly, mDef.Id);
				}

				if (yearly) {
					CreateMeasureDataRecords(Dbc, (int)Intervals.Yearly, mDef.Id);
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
