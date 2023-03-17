using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition.Measure;

[ApiController]
[Route("api/measureDefinition/measure/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get(int measureDefinitionId) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new(),
			intervals = new(),
			measureTypes = new(),
			aggFunctions = AggregationFunctions.list,
			Data = new()
		};

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units.AsNoTracking()) {
				returnObject.units.Add(new UnitsObject { id = unit.Id, name = unit.Name, shortName = unit.Short });
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals.AsNoTracking()) {
				returnObject.intervals.Add(new IntervalsObject { id = interval.Id, name = interval.Name });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTracking()) {
				returnObject.measureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
			}

			foreach (var md in _context.MeasureDefinition.Where(md => md.Id == measureDefinitionId)) {
				var currentMD = new MeasureDefinitionViewModel {
					Id = md.Id,
					MeasureTypeId = md.MeasureTypeId,
					Name = md.Name,
					VarName = md.VariableName,
					Description = md.Description,
					Expression = md.Expression,
					Precision = md.Precision,
					Priority = md.Priority,
					FieldNumber = md.FieldNumber,
					UnitId = md.Unit!.Id,
					Calculated = md.Calculated,

					//find which interval Id to give
					//currentMD.intervalId = Helper.findIntervalId(currentMD, _intervalsRepository);
					IntervalId = md.ReportIntervalId
				};

				bool daily, weekly, monthly, quarterly, yearly = false;

				daily = Helper.nullBoolToBool(md.AggDaily) && currentMD.IntervalId != (int)Helper.intervals.daily;
				weekly = Helper.nullBoolToBool(md.AggWeekly) && currentMD.IntervalId != (int)Helper.intervals.weekly;
				monthly = Helper.nullBoolToBool(md.AggMonthly) && currentMD.IntervalId != (int)Helper.intervals.monthly;
				quarterly = Helper.nullBoolToBool(md.AggQuarterly) && currentMD.IntervalId != (int)Helper.intervals.quarterly;
				yearly = Helper.nullBoolToBool(md.AggYearly) && currentMD.IntervalId != (int)Helper.intervals.yearly;

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
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDefinitionIndexReturnObject> Put(int id2, MeasureDefinitionViewModel value) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new(),
			intervals = new(),
			measureTypes = new(),
			aggFunctions = AggregationFunctions.list,
			Data = new()
		};

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				returnObject.units.Add(new UnitsObject { id = unit.Id, name = unit.Name, shortName = unit.Short });
			}

			foreach (var interval in intervals) {
				returnObject.intervals.Add(new IntervalsObject { id = interval.Id, name = interval.Name });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes) {
				returnObject.measureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
			}

			var mDef = _context.MeasureDefinition.Where(m => m.Id == value.Id).Single();

			// Validates name and variable name
			int validateCount = _context.MeasureDefinition
			  .Where(m =>
					  m.Id != value.Id &&
					  (m.Name.Trim().ToLower() == value.Name.Trim().ToLower() || m.VariableName.Trim().ToLower() == value.VarName.Trim().ToLower())
					)
			  .Count();

			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}


			// Get Values from Page
			if (value.Expression is null) {
				value.Calculated = false;
			}
			else {
				value.Calculated = value.Expression.Trim().Length > 0;
				value.Expression = value.Expression.Replace(" \"", "\"").Replace("\" ", "\"");
			}

			bool daily, weekly, monthly, quarterly, yearly = false;
			daily = Helper.nullBoolToBool(value.Daily) && value.IntervalId != (int)Helper.intervals.daily;
			weekly = Helper.nullBoolToBool(value.Weekly) && value.IntervalId != (int)Helper.intervals.weekly;
			monthly = Helper.nullBoolToBool(value.Monthly) && value.IntervalId != (int)Helper.intervals.monthly;
			quarterly = Helper.nullBoolToBool(value.Quarterly) && value.IntervalId != (int)Helper.intervals.quarterly;
			yearly = Helper.nullBoolToBool(value.Yearly) && value.IntervalId != (int)Helper.intervals.yearly;
			value.AggFunctionId ??= (byte)enumAggFunctions.summation;

			// Check if some values were changed in order to create new measure data records
			var mDef_ = _context.Entry(mDef);
			bool createMeasureData = (int)mDef_.Property("ReportIntervalId").CurrentValue! != value.IntervalId ||
									 mDef.AggDaily != daily ||
									 mDef.AggWeekly != weekly ||
									 mDef.AggMonthly != monthly ||
									 mDef.AggQuarterly != quarterly ||
									 mDef.AggYearly != yearly;

			// Check if some values were changed in order to update IsProcessed to 1 for measure data records 
			bool updateMeasureData = mDef.VariableName != value.VarName ||
									 mDef.Expression != value.Expression ||
									 mDef.AggFunction != value.AggFunctionId ||
									 createMeasureData;

			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			if (value.Id is not null) {
				mDef.Id = (long)value.Id;
			}

			mDef.Name = value.Name;
			mDef_.Property("MeasureTypeId").CurrentValue = value.MeasureTypeId;
			mDef.VariableName = value.VarName;
			mDef.Description = value.Description;
			mDef.Precision = value.Precision;
			mDef.Priority = (short)value.Priority;
			mDef.FieldNumber = value.FieldNumber;
			mDef_.Property("UnitId").CurrentValue = value.UnitId;
			mDef.Calculated = (bool)value.Calculated;
			mDef.Expression = value.Expression;
			mDef_.Property("ReportIntervalId").CurrentValue = value.IntervalId;
			mDef.AggDaily = daily;
			mDef.AggWeekly = weekly;
			mDef.AggMonthly = monthly;
			mDef.AggQuarterly = quarterly;
			mDef.AggYearly = yearly;

			if (daily || weekly || monthly || quarterly || yearly) {
				mDef.AggFunction = value.AggFunctionId;

				if (mDef.Calculated != true && value.AggFunctionId == (byte)enumAggFunctions.expression) {
					mDef.AggFunction = (byte)enumAggFunctions.summation;
				}
			}
			else {
				mDef.AggFunction = null;
			}

			mDef.LastUpdatedOn = lastUpdatedOn;
			mDef.IsProcessed = (byte)Helper.IsProcessed.complete;

			_context.SaveChanges();
			returnObject.Data.Add(value);

			// Update IsProcessed to 1 for Measure Data records
			if (updateMeasureData) {
				Helper.UpdateMeasureDataIsProcessed(_context, mDef.Id, _user.userId);
			}

			// Create Measure Data records for current intervals if they don't exists
			if (createMeasureData) {
				Helper.CreateMeasureDataRecords(_context, value.IntervalId, mDef.Id);
				if (weekly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.weekly, mDef.Id);
				if (monthly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.monthly, mDef.Id);
				if (quarterly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.quarterly, mDef.Id);
				if (yearly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.yearly, mDef.Id);
			}

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-04",
				Resource.MEASURE_DEFINITION,
				@"Updated / ID=" + mDef.Id.ToString(),
				lastUpdatedOn,
				_user.userId
			);

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
