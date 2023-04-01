using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.MeasureDefinition.Measure;

[ApiController]
[Route("api/measureDefinition/measure/[controller]")]
[Authorize(Roles = "RegionalAdministrator, SystemAdministrator")]
public class EditController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public EditController(ApplicationDbContext context) => _context = context;

	[HttpGet("{measureDefinitionId}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Get(int measureDefinitionId) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			Units = new(),
			Intervals = new(),
			MeasureTypes = new(),
			AggFunctions = AggregationFunctions.list,
			Data = new()
		};

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units.AsNoTracking()) {
				returnObject.Units.Add(new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals.AsNoTracking()) {
				returnObject.Intervals.Add(new IntervalsObject { Id = interval.Id, Name = interval.Name });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTracking()) {
				returnObject.MeasureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
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
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}

	[HttpPut("{id}")]
	public ActionResult<MeasureDefinitionIndexReturnObject> Put(int id, MeasureDefinitionViewModel dto) {
		var returnObject = new MeasureDefinitionIndexReturnObject {
			Units = new(),
			Intervals = new(),
			MeasureTypes = new(),
			AggFunctions = AggregationFunctions.list,
			Data = new()
		};

		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var intervals = _context.Interval.OrderBy(i => i.Id);

			var units = _context.Unit.OrderBy(u => u.Id);
			foreach (var unit in units) {
				returnObject.Units.Add(new UnitsObject { Id = unit.Id, Name = unit.Name, ShortName = unit.Short });
			}

			foreach (var interval in intervals) {
				returnObject.Intervals.Add(new IntervalsObject { Id = interval.Id, Name = interval.Name });
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes) {
				returnObject.MeasureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
			}

			var mDef = _context.MeasureDefinition.Where(m => m.Id == dto.Id).Single();

			// Validates name and variable name
			int validateCount = _context.MeasureDefinition
			  .Where(m =>
					  m.Id != dto.Id &&
					  (m.Name.Trim().ToLower() == dto.Name.Trim().ToLower() || m.VariableName.Trim().ToLower() == dto.VarName.Trim().ToLower())
					)
			  .Count();

			if (validateCount > 0) {
				throw new Exception(Resource.VAL_MEASURE_DEF_NAME_EXIST);
			}


			// Get Values from Page
			if (dto.Expression is null) {
				dto.Calculated = false;
			}
			else {
				dto.Calculated = dto.Expression.Trim().Length > 0;
				dto.Expression = dto.Expression.Replace(" \"", "\"").Replace("\" ", "\"");
			}

			bool daily, weekly, monthly, quarterly, yearly = false;
			daily = (dto.Daily ?? false) && dto.IntervalId != (int)Intervals.Daily;
			weekly = (dto.Weekly ?? false) && dto.IntervalId != (int)Intervals.Weekly;
			monthly = (dto.Monthly ?? false) && dto.IntervalId != (int)Intervals.Monthly;
			quarterly = (dto.Quarterly ?? false) && dto.IntervalId != (int)Intervals.Quarterly;
			yearly = (dto.Yearly ?? false) && dto.IntervalId != (int)Intervals.Yearly;
			dto.AggFunctionId ??= (byte)enumAggFunctions.summation;

			// Check if some values were changed in order to create new measure data records
			var mDef_ = _context.Entry(mDef);
			bool createMeasureData = (int)mDef_.Property("ReportIntervalId").CurrentValue! != dto.IntervalId ||
									 mDef.AggDaily != daily ||
									 mDef.AggWeekly != weekly ||
									 mDef.AggMonthly != monthly ||
									 mDef.AggQuarterly != quarterly ||
									 mDef.AggYearly != yearly;

			// Check if some values were changed in order to update IsProcessed to 1 for measure data records
			bool updateMeasureData = mDef.VariableName != dto.VarName ||
									 mDef.Expression != dto.Expression ||
									 mDef.AggFunction != dto.AggFunctionId ||
									 createMeasureData;

			var lastUpdatedOn = DateTime.Now;

			// Set values from page
			if (dto.Id is not null) {
				mDef.Id = (long)dto.Id;
			}

			mDef.Name = dto.Name;
			mDef_.Property("MeasureTypeId").CurrentValue = dto.MeasureTypeId;
			mDef.VariableName = dto.VarName;
			mDef.Description = dto.Description;
			mDef.Precision = dto.Precision;
			mDef.Priority = (short)dto.Priority;
			mDef.FieldNumber = dto.FieldNumber;
			mDef_.Property("UnitId").CurrentValue = dto.UnitId;
			mDef.Calculated = (bool)dto.Calculated;
			mDef.Expression = dto.Expression;
			mDef_.Property("ReportIntervalId").CurrentValue = dto.IntervalId;
			mDef.AggDaily = daily;
			mDef.AggWeekly = weekly;
			mDef.AggMonthly = monthly;
			mDef.AggQuarterly = quarterly;
			mDef.AggYearly = yearly;

			if (daily || weekly || monthly || quarterly || yearly) {
				mDef.AggFunction = dto.AggFunctionId;

				if (mDef.Calculated != true && dto.AggFunctionId == (byte)enumAggFunctions.expression) {
					mDef.AggFunction = (byte)enumAggFunctions.summation;
				}
			}
			else {
				mDef.AggFunction = null;
			}

			mDef.LastUpdatedOn = lastUpdatedOn;
			mDef.IsProcessed = (byte)IsProcessed.complete;

			_context.SaveChanges();
			returnObject.Data.Add(dto);

			// Update IsProcessed to 1 for Measure Data records
			if (updateMeasureData) {
				UpdateMeasureDataIsProcessed(_context, mDef.Id, _user.Id);
			}

			// Create Measure Data records for current intervals if they don't exists
			if (createMeasureData) {
				CreateMeasureDataRecords(_context, dto.IntervalId, mDef.Id);
				if (weekly)
					CreateMeasureDataRecords(_context, (int)Intervals.Weekly, mDef.Id);
				if (monthly)
					CreateMeasureDataRecords(_context, (int)Intervals.Monthly, mDef.Id);
				if (quarterly)
					CreateMeasureDataRecords(_context, (int)Intervals.Quarterly, mDef.Id);
				if (yearly)
					CreateMeasureDataRecords(_context, (int)Intervals.Yearly, mDef.Id);
			}

			AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-04",
				Resource.MEASURE_DEFINITION,
				@"Updated / ID=" + mDef.Id.ToString(),
				lastUpdatedOn,
				_user.Id
			);

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_context, e, _user.Id));
		}
	}
}
