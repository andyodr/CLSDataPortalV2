﻿using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.MeasureData;

[ApiController]
[Route("api/measuredata/[controller]")]
[Authorize]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	/// <summary>
	/// Get a MeasureDataIndexListObject
	/// </summary>
	[HttpGet]
	public ActionResult<MeasureDataIndexListObject> Get([FromQuery] MeasureDataReceiveObject value) {
		var returnObject = new MeasureDataIndexListObject { data = new() };
		DateTime? date = new();

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			returnObject.allow = _user.hierarchyIds.Contains(value.HierarchyId);

			//this is for a special case where some level 2 hierarchies can not be edited since they are a sum value
			returnObject.editValue = Helper.CanEditValueFromSpecialHierarchy(_config, value.HierarchyId);

			returnObject.calendarId = value.CalendarId;
			if (string.IsNullOrEmpty(value.Day)) {
				date = null;
			}
			else {
				date = Convert.ToDateTime(value.Day);
				var cal = _context.Calendar
					.Where(c => c.StartDate == date)
					.AsNoTrackingWithIdentityResolution().ToArray();
				if (cal.Length > 0) {
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
						   where m.Active == true && m.Hierarchy.Id == value.HierarchyId
						   && mdef.MeasureType.Id == value.MeasureTypeId
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
							   calculated = Helper.IsMeasureCalculated(_context, m.Expression ?? false, value.HierarchyId, calendar.Interval.Id, md.Id, null)
						   };

			// Find all measure definition variables.
			var allVarNames = from m in _context.MeasureDefinition.Where(m => m.MeasureType.Id == value.MeasureTypeId)
							  select new { m.Id, m.VariableName };

			foreach (var record in measures.AsNoTracking()) {
				var newObject = new MeasureDataReturnObject();

				if (record.User is null) {
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

				if (record.target is not null) {
					newObject.target = Math.Round((double)record.target, record.precision, MidpointRounding.AwayFromZero);
				}

				if (record.yellow is not null) {
					newObject.yellow = Math.Round((double)record.yellow, record.precision, MidpointRounding.AwayFromZero);
				}

				if (record.value is not null) {
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
							var measure = _context.Measure
								.Where(m => m.MeasureDefinition.Id == item.id && m.Hierarchy.Id == value.HierarchyId)
								.AsNoTracking().ToArray();
							if (measure.Length > 0) {
								var measureData = _context.MeasureData
									.Where(md => md.Measure.Id == measure.First().Id && md.Calendar.Id == returnObject.calendarId)
									.AsNoTracking().ToArray();
								if (measureData.Length > 0) {
									if (measureData.First().Value is not null) {
										sExpression = sExpression.Replace("Data[\"" + item.varName + "\"]", measureData.First().Value.ToString());
									}
								}
							}
						}

						newObject.evaluated = sExpression;
					}
				}

				returnObject.data.Add(newObject);
			}

			returnObject.range = BuildRangeString(value.CalendarId);
			if (value.CalendarId != _user.savedFilters[Helper.pages.measureData].calendarId) {
				_user.savedFilters[Helper.pages.measureData].calendarId = value.CalendarId;
				_user.savedFilters[Helper.pages.measureData].intervalId = calendar.Interval.Id;
				_user.savedFilters[Helper.pages.measureData].year = calendar.Year;
			}
			_user.savedFilters[Helper.pages.measureData].hierarchyId = value.HierarchyId;
			returnObject.filter = _user.savedFilters[Helper.pages.measureData];

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexListObject> Put(MeasureDataReceiveObject value) {
		var returnObject = new MeasureDataIndexListObject();
		List<MeasureDataReturnObject> measureDataList = new();

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			//apply precision and validate unit if value != null 
			if (value.MeasureValue is not null) {
				var precision = from md in _context.MeasureData.Where(md => md.Id == value.MeasureDataId)
								join m in _context.Measure
								on md.Measure!.Id equals m.Id
								join mdef in _context.MeasureDefinition
								on m.MeasureDefinition!.Id equals mdef.Id
								select new { mdef.Precision, mdef.Unit };

				var mData = precision.AsNoTracking().First();
				if (mData.Unit.Id == 1 && (value.MeasureValue < 0d || value.MeasureValue > 1d)) {
					throw new Exception(Resource.VAL_VALUE_UNIT);
				}

				//round to precision value
				value.MeasureValue = Math.Round((double)value.MeasureValue, mData.Precision, MidpointRounding.AwayFromZero);
			}

			var lastUpdatedOn = DateTime.Now;
			var measureData = _context.MeasureData.Where(m => m.Id == value.MeasureDataId).First();
			measureData.Value = value.MeasureValue;
			measureData.Explanation = value.Explanation;
			measureData.Action = value.Action;
			_context.Entry(measureData).Property("UserId").CurrentValue = _user.userId;
			measureData.IsProcessed = (byte)Helper.IsProcessed.measureData;
			measureData.LastUpdatedOn = lastUpdatedOn;
			_context.SaveChanges();

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-01",
				Resource.MEASURE_DATA,
				@"Updated / ID=" + value.MeasureDataId.ToString() +
						" / Value=" + value.MeasureValue.ToString() +
						" / Explanation=" + value.Explanation +
						" / Action=" + value.Action,
				lastUpdatedOn,
				_user.userId
			);

			measureDataList.Add(
			  new MeasureDataReturnObject {
				  value = value.MeasureValue,
				  explanation = value.Explanation,
				  action = value.Action,
				  updated = Helper.LastUpdatedOnObj(DateTime.Now, _user.userName)
			  }
			);
			returnObject.data = measureDataList;
			return returnObject;

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
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}

	private string BuildRangeString(int? calendarID) {
		int interval = -1;
		var cal = _context.Calendar.AsNoTracking().Where(c => c.Id == calendarID);
		if (calendarID is null) {
			interval = (int)Helper.intervals.daily;
		}
		else {
			interval = cal.Where(c => c.Id == calendarID).Include(c => c.Interval).First().Interval.Id;
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
