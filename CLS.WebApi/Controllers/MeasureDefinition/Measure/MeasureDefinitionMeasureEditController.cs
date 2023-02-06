using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLS.WebApi.Controllers.MeasureDefinition.Measure;

[Route("api/measureDefinition/measure/[controller]")]
[Authorize]
[ApiController]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public EditController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get(int measureDefinitionId) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new List<UnitsObject>(),
			intervals = new(),
			measureTypes = new(),
			aggFunctions = AggregationFunctions.list,
			data = new List<MeasureDefinitionViewModel>()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) { throw new Exception(); }
			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
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
					id = md.Id,
					measureTypeId = md.MeasureTypeId,
					name = md.Name,
					varName = md.VariableName,
					description = md.Description,
					expression = md.Expression,
					precision = md.Precision,
					priority = md.Priority,
					fieldNumber = md.FieldNumber,
					unitId = md.Unit!.Id,
					calculated = md.Calculated,

					//find which interval Id to give
					//currentMD.intervalId = Helper.findIntervalId(currentMD, _intervalsRepository);
					intervalId = md.ReportIntervalId
				};

				bool daily, weekly, monthly, quarterly, yearly = false;

				daily = Helper.nullBoolToBool(md.AggDaily) && currentMD.intervalId != (int)Helper.intervals.daily;
				weekly = Helper.nullBoolToBool(md.AggWeekly) && currentMD.intervalId != (int)Helper.intervals.weekly;
				monthly = Helper.nullBoolToBool(md.AggMonthly) && currentMD.intervalId != (int)Helper.intervals.monthly;
				quarterly = Helper.nullBoolToBool(md.AggQuarterly) && currentMD.intervalId != (int)Helper.intervals.quarterly;
				yearly = Helper.nullBoolToBool(md.AggYearly) && currentMD.intervalId != (int)Helper.intervals.yearly;

				currentMD.daily = daily;
				currentMD.weekly = weekly;
				currentMD.monthly = monthly;
				currentMD.quarterly = quarterly;
				currentMD.yearly = yearly;
				currentMD.aggFunctionId = md.AggFunction;
				returnObject.data.Add(currentMD);
			}
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
	public ActionResult<JsonResult> Put(int id2, [FromBody] MeasureDefinitionViewModel value) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new List<UnitsObject>(),
			intervals = new(),
			measureTypes = new(),
			aggFunctions = AggregationFunctions.list,
			data = new List<MeasureDefinitionViewModel>()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

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

			var mDef = _context.MeasureDefinition.Where(m => m.Id == value.id).Single();

			// Validates name and variable name
			int validateCount = _context.MeasureDefinition
			  .Where(m =>
					  m.Id != value.id &&
					  (m.Name.Trim().ToLower() == value.name.Trim().ToLower() || m.VariableName.Trim().ToLower() == value.varName.Trim().ToLower())
					)
			  .Count();

			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}


			// Get Values from Page
			if (value.expression == null) {
				value.calculated = false;
			}
			else {
				value.calculated = value.expression.Trim().Length > 0;
				value.expression = value.expression.Replace(" \"", "\"").Replace("\" ", "\"");
			}

			bool daily, weekly, monthly, quarterly, yearly = false;
			daily = Helper.nullBoolToBool(value.daily) && value.intervalId != (int)Helper.intervals.daily;
			weekly = Helper.nullBoolToBool(value.weekly) && value.intervalId != (int)Helper.intervals.weekly;
			monthly = Helper.nullBoolToBool(value.monthly) && value.intervalId != (int)Helper.intervals.monthly;
			quarterly = Helper.nullBoolToBool(value.quarterly) && value.intervalId != (int)Helper.intervals.quarterly;
			yearly = Helper.nullBoolToBool(value.yearly) && value.intervalId != (int)Helper.intervals.yearly;
			value.aggFunctionId ??= (byte)enumAggFunctions.summation;

			// Check if some values were changed in order to create new measure data records
			var mDef_ = _context.Entry(mDef);
			bool createMeasureData = (int)mDef_.Property("ReportIntervalId").CurrentValue! != value.intervalId ||
									 mDef.AggDaily != daily ||
									 mDef.AggWeekly != weekly ||
									 mDef.AggMonthly != monthly ||
									 mDef.AggQuarterly != quarterly ||
									 mDef.AggYearly != yearly;

			// Check if some values were changed in order to update IsProcessed to 1 for measure data records 
			bool updateMeasureData = mDef.VariableName != value.varName ||
									 mDef.Expression != value.expression ||
									 mDef.AggFunction != value.aggFunctionId ||
									 createMeasureData;

			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			if (value.id != null) {
				mDef.Id = (long)value.id;
			}

			mDef.Name = value.name;
			mDef_.Property("MeasureTypeId").CurrentValue = value.measureTypeId;
			mDef.VariableName = value.varName;
			mDef.Description = value.description;
			mDef.Precision = value.precision;
			mDef.Priority = (short)value.priority;
			mDef.FieldNumber = value.fieldNumber;
			mDef_.Property("UnitId").CurrentValue = value.unitId;
			mDef.Calculated = (bool)value.calculated;
			mDef.Expression = value.expression;
			mDef_.Property("ReportIntervalId").CurrentValue = value.intervalId;
			mDef.AggDaily = daily;
			mDef.AggWeekly = weekly;
			mDef.AggMonthly = monthly;
			mDef.AggQuarterly = quarterly;
			mDef.AggYearly = yearly;

			if (daily || weekly || monthly || quarterly || yearly) {
				mDef.AggFunction = value.aggFunctionId;

				if (mDef.Calculated != true && value.aggFunctionId == (byte)enumAggFunctions.expression) {
					mDef.AggFunction = (byte)enumAggFunctions.summation;
				}
			}
			else {
				mDef.AggFunction = null;
			}

			mDef.LastUpdatedOn = lastUpdatedOn;
			mDef.IsProcessed = (byte)Helper.IsProcessed.complete;

			_context.SaveChanges();
			returnObject.data.Add(value);

			// Update IsProcessed to 1 for Measure Data records
			if (updateMeasureData) {
				Helper.UpdateMeasureDataIsProcessed(_context, mDef.Id, _user.userId);
			}

			// Create Measure Data records for current intervals if they don't exists
			if (createMeasureData) {
				Helper.CreateMeasureDataRecords(_context, value.intervalId, mDef.Id);
				if (weekly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.weekly, mDef.Id);
				if (monthly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.monthly, mDef.Id);
				if (quarterly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.quarterly, mDef.Id);
				if (yearly)
					Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.yearly, mDef.Id);
			}

			Helper.addAuditTrail(
			  Resource.WEB_PAGES,
			   "WEB-04",
			   Resource.MEASURE_DEFINITION,
			   @"Updated / ID=" + mDef.Id.ToString(),
			   lastUpdatedOn,
			   _user.userId
			);

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
