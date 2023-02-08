using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace CLS.WebApi.Controllers.MeasureData;

[Route("api/measuredata/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(MeasureDataReceiveObject value) {
		var returnObject = new MeasureDataIndexListObject();
		List<MeasureDataReturnObject> measureDataList = new();
		DateTime? date = new();
		List<long> id = new();

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.measureData, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			returnObject.allow = _user.hierarchyIds.Contains(value.hierarchyId);

			//this is for a special case where some level 2 hierarchies can not be edited since they are a sum value
			returnObject.editValue = Helper.CanEditValueFromSpecialHierarchy(_config, value.hierarchyId);

			returnObject.calendarId = value.calendarId;
			if (value.day == null || value.day.Equals("")) {
				date = null;
			}
			else {
				date = Convert.ToDateTime(value.day);
				var cal = _context.Calendar.Where(c => c.StartDate == date).AsNoTracking().ToList();
				if (cal.Count > 0) {
					returnObject.calendarId = cal.First().Id;
				}
			}

			var calendar = _context.Calendar
				.Where(c => c.Id == returnObject.calendarId)
				.Include(c => c.Interval)
				.AsNoTrackingWithIdentityResolution().First();

			returnObject.locked = false;
			// From settings page, DO NOT USE = !Active
			if (_context.Setting.AsNoTracking().First().Active == true) {
				returnObject.locked = Helper.IsDataLocked(calendar.Interval.Id, _user.userId, calendar, _context);
			}

			var measures = from mdef in _context.MeasureDefinition
						   from m in mdef.Measures
						   from t in m.Targets
						   from md in t.MeasureData
						   where m.Active == true && m.Hierarchy.Id == value.hierarchyId
						   && mdef.MeasureType.Id == value.measureTypeId
						   && md.CalendarId == returnObject.calendarId
						   select new {
							   lastUpdatedOn = md.LastUpdatedOn,
							   md.User,
							   value = md.Value,
							   target = t.Value,
							   yellow = t.YellowValue,
							   id = md.Id,
							   name = mdef.Name,
							   precision = mdef.Precision,
							   description = mdef.Description,
							   expression = mdef.Expression,
							   mdef.Unit,
							   explanation = md.Explanation,
							   action = md.Action,
							   hId = m.Hierarchy.Id,
							   calculated = Helper.IsMeasureCalculated(_context, m.Expression ?? false, value.hierarchyId, calendar.Interval.Id, md.Id, null)
						   };

			// Find all measure definition variables.
			var allVarNames = from m in _context.MeasureDefinition.Where(m => m.MeasureType.Id == value.measureTypeId)
							  select new { m.Id, m.VariableName };

			foreach (var record in measures.AsNoTracking()) {
				var newObject = new MeasureDataReturnObject();

				if (record.User == null) {
					newObject.updated = Helper.LastUpdatedOnObj(record.lastUpdatedOn, Resource.SYSTEM);
				}
				else {
					newObject.updated = Helper.LastUpdatedOnObj(record.lastUpdatedOn, record.User.UserName);
				}

				newObject.id = record.id;
				newObject.name = record.name;
				newObject.explanation = record.explanation;
				newObject.description = record.description;
				newObject.action = record.action;
				newObject.unitId = record.Unit.Id;
				newObject.units = record.Unit.Short;
				newObject.target = record.target;
				newObject.yellow = record.yellow;
				newObject.value = record.value;

				if (record.target != null) {
					newObject.target = Math.Round((double)record.target, record.precision, MidpointRounding.AwayFromZero);
				}

				if (record.yellow != null) {
					newObject.yellow = Math.Round((double)record.yellow, record.precision, MidpointRounding.AwayFromZero);
				}

				if (record.value != null) {
					newObject.value = Math.Round((double)record.value, record.precision, MidpointRounding.AwayFromZero);
				}

				newObject.calculated = record.calculated;

				// Evaluates expressions
				newObject.expression = record.expression;
				newObject.evaluated = string.Empty;
				if (newObject.calculated && !string.IsNullOrEmpty(newObject.expression)) {
					var varNames = new List<VariableName>();
					foreach (var item in allVarNames) {
						if (newObject.expression.Contains(item.VariableName)) {
							varNames.Add(new VariableName { id = item.Id, varName = item.VariableName });
						}
					}
					// Search measure data values from variables
					if (varNames.Count > 0) {
						string sExpression = newObject.expression;
						foreach (var item in varNames) {
							var measure = _context.Measure.Where(m => m.MeasureDefinition.Id == item.id && m.Hierarchy.Id == value.hierarchyId).AsNoTracking().ToList();
							if (measure.Count > 0) {
								var measureData = _context.MeasureData.Where(md => md.Measure.Id == measure.First().Id && md.Calendar.Id == returnObject.calendarId).AsNoTracking().ToList();
								if (measureData.Count > 0) {
									if (measureData.First().Value != null) {
										sExpression = sExpression.Replace("Data[\"" + item.varName + "\"]", measureData.First().Value.ToString());
									}
								}
							}
						}

						newObject.evaluated = sExpression;
					}
				}

				measureDataList.Add(newObject);
			}

			returnObject.range = BuildRangeString(value.calendarId);
			returnObject.data = measureDataList;

			if (value.calendarId != _user.savedFilters[Helper.pages.measureData].calendarId) {
				_user.savedFilters[Helper.pages.measureData].calendarId = value.calendarId;
				_user.savedFilters[Helper.pages.measureData].intervalId = calendar.Interval.Id;
				_user.savedFilters[Helper.pages.measureData].year = calendar.Year;
			}
			_user.savedFilters[Helper.pages.measureData].hierarchyId = value.hierarchyId;
			returnObject.filter = _user.savedFilters[Helper.pages.measureData];

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) => "value";

	[HttpPost]
	public void Post([FromBody] MeasureDataReceiveObject value) {
	}

	[HttpPut]
	public ActionResult<JsonObject> Put([FromBody] MeasureDataReceiveObject value) {
		var returnObject = new MeasureDataIndexListObject();
		List<MeasureDataReturnObject> measureDataList = new();

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			//apply precision and validate unit if value != null 
			if (value.measureValue != null) {
				var precision = from md in _context.MeasureData.Where(md => md.Id == value.measureDataId)
								join m in _context.Measure
								on md.Measure!.Id equals m.Id
								join mdef in _context.MeasureDefinition
								on m.MeasureDefinition!.Id equals mdef.Id
								select new { mdef.Precision, mdef.Unit };

				var mData = precision.AsNoTracking().First();
				if (mData.Unit.Id == 1 && (value.measureValue < 0d || value.measureValue > 1d)) {
					throw new Exception(Resource.VAL_VALUE_UNIT);
				}

				//round to precision value
				value.measureValue = Math.Round((double)value.measureValue, mData.Precision, MidpointRounding.AwayFromZero);
			}

			var lastUpdatedOn = DateTime.Now;
			var measureData = _context.MeasureData.Where(m => m.Id == value.measureDataId).First();
			measureData.Value = value.measureValue;
			measureData.Explanation = value.explanation;
			measureData.Action = value.action;
			_context.Entry(measureData).Property("UserId").CurrentValue = _user.userId;
			measureData.IsProcessed = (byte)Helper.IsProcessed.measureData;
			measureData.LastUpdatedOn = lastUpdatedOn;
			_context.SaveChanges();

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-01",
				Resource.MEASURE_DATA,
				@"Updated / ID=" + value.measureDataId.ToString() +
						" / Value=" + value.measureValue.ToString() +
						" / Explanation=" + value.explanation +
						" / Action=" + value.action,
				lastUpdatedOn,
				_user.userId
			);

			measureDataList.Add(
			  new MeasureDataReturnObject {
				  value = value.measureValue,
				  explanation = value.explanation,
				  action = value.action,
				  updated = Helper.LastUpdatedOnObj(DateTime.Now, _user.userName)
			  }
			);
			returnObject.data = measureDataList;
			return new JsonResult(returnObject);

			//var measureData = _measureDataRepository.All().Where(m => m.Id == value.measureDataId);
			//foreach (var metricData in measureData)
			//{
			//  metricData.Value = value.measureValue;
			//  metricData.Explanation = value.explanation;
			//  metricData.Action = value.action;
			//  metricData.UserId = _user.userId;
			//  metricData.LastUpdatedOn = DateTime.Now;
			//}
			//_measureDataRepository.SaveChanges();
			//return Get(value);

		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}

	private string BuildRangeString(int? calendarID) {
		int interval = -1;
		var cal = _context.Calendar.AsNoTracking().Where(c => c.Id == calendarID);
		if (calendarID == null) {
			interval = (int)Helper.intervals.daily;
		}
		else {
			interval = cal.Where(c => c.Id == calendarID).First().Interval.Id;
		}

		if (interval == (int)Helper.intervals.daily) {
			var date = cal.First().StartDate ?? new DateTime(0);
			return date.ToString("MMM-dd-yyyy");
		}
		else if (interval == (int)Helper.intervals.weekly || interval == (int)Helper.intervals.quarterly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";

		}
		else if (interval == (int)Helper.intervals.monthly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";
		}
		else if (interval == (int)Helper.intervals.yearly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";
		}
		else {
			throw new Exception(Resource.VAL_INVALID_INTERVAL_ID);
		}
	}
}
