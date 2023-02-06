using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.MeasureDefinition.Measure;

[Route("api/measureDefinition/measure/[controller]")]
[Authorize]
[ApiController]
public class AddController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public AddController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get() {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			units = new(),
			intervals = new(),
			aggFunctions = AggregationFunctions.list,
			measureTypes = new()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

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

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	[HttpPost]
	public ActionResult<JsonResult> Post([FromBody] MeasureDefinitionViewModel value) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureDefinition, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var result = new MeasureDefinitionIndexReturnObject {
				units = new List<UnitsObject>(),
				intervals = new(),
				measureTypes = new(),
				data = new List<MeasureDefinitionViewModel>()
			};

			// Validates name and variable name
			int validateCount = _context.MeasureDefinition
			  .Where(m =>
				m.Name.Trim().ToLower() == value.name.Trim().ToLower() ||
				m.VariableName.Trim().ToLower() == value.varName.Trim().ToLower()).Count();

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
			if (value.expression == null)
				value.calculated = false;
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
			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			var currentMD = new Data.Models.MeasureDefinition {
				Name = value.name,
				VariableName = value.varName,
				Description = value.description,
				Precision = value.precision,
				Priority = (short)value.priority,
				FieldNumber = value.fieldNumber,
				Calculated = (bool)value.calculated,
				Expression = value.expression,
				AggDaily = daily,
				AggWeekly = weekly,
				AggMonthly = monthly,
				AggQuarterly = quarterly,
				AggYearly = yearly
			};

			if (daily || weekly || monthly || quarterly || yearly) {
				currentMD.AggFunction = value.aggFunctionId;
				if (currentMD.Calculated != true && value.aggFunctionId == (byte)enumAggFunctions.expression) {
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
			value.id = currentMD.Id;
			result.data.Add(value);

			// Create Measure and Target records
			string measuresAndTargets = Helper.CreateMeasuresAndTargets(_context, _user.userId, value);
			if (!string.IsNullOrEmpty(measuresAndTargets)) {
				throw new Exception(measuresAndTargets);
			}

			// Create Measure Data records for current intervals
			Helper.CreateMeasureDataRecords(_context, value.intervalId, currentMD.Id);
			if (weekly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.weekly, currentMD.Id);
			if (monthly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.monthly, currentMD.Id);
			if (quarterly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.quarterly, currentMD.Id);
			if (yearly)
				Helper.CreateMeasureDataRecords(_context, (int)Helper.intervals.yearly, currentMD.Id);

			Helper.addAuditTrail(
			  Resource.WEB_PAGES,
			   "WEB-04",
			   Resource.MEASURE_DEFINITION,
			   @"Added / ID=" + currentMD.Id.ToString(),
			   lastUpdatedOn,
			   _user.userId
			);

			return new JsonResult(result);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
