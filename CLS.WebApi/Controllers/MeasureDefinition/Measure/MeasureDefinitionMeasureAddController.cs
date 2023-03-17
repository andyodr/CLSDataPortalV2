using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Measure;

[ApiController]
[Route("api/measureDefinition/measure/[controller]")]
[Authorize(Roles = "Regional Administrator, System Administrator")]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public AddController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get() {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new(),
			intervals = new(),
			aggFunctions = AggregationFunctions.list,
			measureTypes = new()
		};

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals) {
				returnObject.intervals.Add(new IntervalsObject { id = interval.Id, name = interval.Name });
			}

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				returnObject.units.Add(new UnitsObject { id = unit.Id, name = unit.Name, shortName = unit.Short });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var mt in measureTypes) {
				returnObject.measureTypes.Add(new MeasureTypeFilterObject { Id = mt.Id, Name = mt.Name });
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPost]
	public ActionResult<MeasureDefinitionIndexReturnObject> Post(MeasureDefinitionViewModel value) {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var result = new MeasureDefinitionIndexReturnObject {
				units = new List<UnitsObject>(),
				intervals = new(),
				measureTypes = new(),
				Data = new List<MeasureDefinitionViewModel>()
			};

			// Validates name and variable name
			int validateCount = _context.MeasureDefinition
			  .Where(m =>
				m.Name.Trim().ToLower() == value.Name.Trim().ToLower() ||
				m.VariableName.Trim().ToLower() == value.VarName.Trim().ToLower()).Count();

			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals) {
				result.intervals.Add(new IntervalsObject { id = interval.Id, name = interval.Name });
			}

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				result.units.Add(new UnitsObject { id = unit.Id, name = unit.Name, shortName = unit.Short });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes) {
				result.measureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
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
			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			var currentMD = new Data.Models.MeasureDefinition {
				Name = value.Name,
				VariableName = value.VarName,
				Description = value.Description,
				Precision = value.Precision,
				Priority = (short)value.Priority,
				FieldNumber = value.FieldNumber,
				Calculated = (bool)value.Calculated,
				Expression = value.Expression,
				AggDaily = daily,
				AggWeekly = weekly,
				AggMonthly = monthly,
				AggQuarterly = quarterly,
				AggYearly = yearly
			};

			if (daily || weekly || monthly || quarterly || yearly) {
				currentMD.AggFunction = value.AggFunctionId;
				if (currentMD.Calculated != true && value.AggFunctionId == (byte)enumAggFunctions.expression) {
					currentMD.AggFunction = (byte)enumAggFunctions.summation;
				}
			}
			else {
				currentMD.AggFunction = null;
			}

			currentMD.LastUpdatedOn = lastUpdatedOn;
			currentMD.IsProcessed = (byte)Helper.IsProcessed.complete;

			var test = _context.MeasureDefinition.Add(currentMD);
			_context.SaveChanges();
			value.Id = currentMD.Id;
			result.Data.Add(value);

			// Create Measure and Target records
			string measuresAndTargets = Helper.CreateMeasuresAndTargets(_context, _user.userId, value);
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			// Create Measure Data records for current intervals
			Helper.CreateMeasureDataRecords(_context, value.IntervalId, currentMD.Id);
			if (weekly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.weekly, currentMD.Id);
			if (monthly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.monthly, currentMD.Id);
			if (quarterly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.quarterly, currentMD.Id);
			if (yearly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.yearly, currentMD.Id);

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-04",
				Resource.MEASURE_DEFINITION,
				@"Added / ID=" + currentMD.Id.ToString(),
				lastUpdatedOn,
				_user.userId
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
