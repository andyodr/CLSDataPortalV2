using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureData;

public record VariableName(long Id, string VarName);

[ApiController]
[Route("api/measuredata/[controller]")]
[Authorize]
public sealed class IndexController : BaseController
{
	/// <summary>
	/// Get a MeasureDataIndexListObject
	/// </summary>
	[HttpGet]
	public ActionResult<MeasureDataIndexListObject> Get([FromQuery] MeasureDataReceiveObject dto) {
		MeasureDataIndexListObject result = new() { Data = new List<MeasureDataReturnObject>() };
		DateTime? date = new();
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			result.Allow = Dbc.UserHierarchy
				.Where(d => d.UserId == _user.Id && d.HierarchyId == dto.HierarchyId).Any();

			//this is for a special case where some level 2 hierarchies can not be edited since they are a sum value
			result.EditValue = CanEditValueFromSpecialHierarchy(Config, dto.HierarchyId);

			result.CalendarId = dto.CalendarId;
			if (string.IsNullOrEmpty(dto.Day)) {
				date = null;
			}
			else {
				date = Convert.ToDateTime(dto.Day);
				var cal = Dbc.Calendar
					.Where(c => c.StartDate == date)
					.AsNoTrackingWithIdentityResolution().ToArray();
				if (cal.Length > 0) {
					result.CalendarId = cal.First().Id;
				}
			}

			var calendar = Dbc.Calendar
				.Where(c => c.Id == result.CalendarId)
				.Include(c => c.Interval)
				.AsNoTrackingWithIdentityResolution().First();

			result.Locked = false;
			// From settings page, DO NOT USE = !Active
			if (Dbc.Setting.AsNoTracking().First().Active == true) {
				result.Locked = IsDataLocked(calendar.Interval.Id, _user.Id, calendar, Dbc);
			}

			var measures = Dbc.MeasureData
				.Where(m => m.Measure!.Active == true
					&& m.Measure.HierarchyId == dto.HierarchyId
					&& m.Measure.MeasureDefinition!.MeasureTypeId == dto.MeasureTypeId
					&& m.CalendarId == result.CalendarId)
				.Include(md => md.Target)
				.Include(md => md.User)
				.Include(md => md.Measure)
				.ThenInclude(m => m!.MeasureDefinition)
				.Select(md => new {
					lastUpdatedOn = md.LastUpdatedOn,
					md.User,
					md.Value,
					target = md.Target!.Value,
					md.Target.YellowValue,
					md.Id,
					md.Measure!.MeasureDefinition!.Name,
					md.Measure.MeasureDefinition.Precision,
					md.Measure.MeasureDefinition.Description,
					md.Measure.MeasureDefinition.Expression,
					md.Measure.MeasureDefinition.Unit,
					md.Explanation,
					md.Action,
					md.Measure.HierarchyId,
					calculated = IsMeasureCalculated(Dbc, md.Measure.Expression ?? false, dto.HierarchyId, calendar.Interval.Id, md.Measure.MeasureDefinition.Id, null)
				});

			// Find all measure definition variables.
			var allVarNames = from m in Dbc.MeasureDefinition.Where(m => m.MeasureType.Id == dto.MeasureTypeId)
							  select new { m.Id, m.VariableName };

			foreach (var record in measures.AsNoTrackingWithIdentityResolution()) {
				var measureDataDto = new MeasureDataReturnObject {
					Id = record.Id,
					Name = record.Name,
					Explanation = record.Explanation,
					Description = record.Description,
					Action = record.Action,
					UnitId = record.Unit.Id,
					Units = record.Unit.Short,
					Target = record.target,
					Yellow = record.YellowValue,
					Value = record.Value,
					Calculated = record.calculated,
					Expression = record.Expression,
					Evaluated = String.Empty,
					Updated = LastUpdatedOnObj(record.lastUpdatedOn, record.User?.UserName ?? Resource.SYSTEM)
				};

				if (record.target is not null) {
					measureDataDto.Target = Math.Round((double)record.target, record.Precision, MidpointRounding.AwayFromZero);
				}

				if (record.YellowValue is not null) {
					measureDataDto.Yellow = Math.Round((double)record.YellowValue, record.Precision, MidpointRounding.AwayFromZero);
				}

				if (record.Value is not null) {
					measureDataDto.Value = Math.Round((double)record.Value, record.Precision, MidpointRounding.AwayFromZero);
				}

				if (measureDataDto.Calculated && !string.IsNullOrEmpty(measureDataDto.Expression)) {
					var varNames = new List<VariableName>();
					foreach (var item in allVarNames) {
						if (measureDataDto.Expression.Contains(item.VariableName)) {
							varNames.Add(new VariableName(item.Id, item.VariableName));
						}
					}
					// Search measure data values from variables
					if (varNames.Count > 0) {
						string sExpression = measureDataDto.Expression;
						foreach (var item in varNames) {
							var measure = Dbc.Measure
								.Where(m => m.MeasureDefinitionId == item.Id && m.HierarchyId == dto.HierarchyId)
								.AsNoTracking().ToArray();
							if (measure.Length > 0) {
								var measureData = Dbc.MeasureData
									.Where(md => md.Measure!.Id == measure.First().Id && md.CalendarId == result.CalendarId)
									.AsNoTracking().ToArray();
								if (measureData.Length > 0) {
									if (measureData.First().Value is not null) {
										sExpression = sExpression.Replace("Data[\"" + item.VarName + "\"]", measureData.First().Value.ToString());
									}
								}
							}
						}

						measureDataDto.Evaluated = sExpression;
					}
				}

				result.Data.Add(measureDataDto);
			}

			result.Range = BuildRangeString(dto.CalendarId);
			if (dto.CalendarId != _user.savedFilters[Pages.MeasureData].calendarId) {
				_user.savedFilters[Pages.MeasureData].calendarId = dto.CalendarId;
				_user.savedFilters[Pages.MeasureData].intervalId = calendar.Interval.Id;
				_user.savedFilters[Pages.MeasureData].year = calendar.Year;
			}
			_user.savedFilters[Pages.MeasureData].hierarchyId = dto.HierarchyId;
			result.Filter = _user.savedFilters[Pages.MeasureData];

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexListObject> Put(MeasureDataReceiveObject value) {
		MeasureDataIndexListObject result = new();
		List<MeasureDataReturnObject> measureDataList = new();
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			//apply precision and validate unit if value != null
			if (value.MeasureValue is not null) {
				var precision = from md in Dbc.MeasureData.Where(md => md.Id == value.MeasureDataId)
								join m in Dbc.Measure
								on md.Measure!.Id equals m.Id
								join mdef in Dbc.MeasureDefinition
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
			var measureData = Dbc.MeasureData.Where(m => m.Id == value.MeasureDataId).First();
			measureData.Value = value.MeasureValue;
			measureData.Explanation = value.Explanation;
			measureData.Action = value.Action;
			Dbc.Entry(measureData).Property("UserId").CurrentValue = _user.Id;
			measureData.IsProcessed = (byte)IsProcessed.MeasureData;
			measureData.LastUpdatedOn = lastUpdatedOn;
			Dbc.SaveChanges();

			AddAuditTrail(Dbc,
				Resource.WEB_PAGES,
				"WEB-01",
				Resource.MEASURE_DATA,
				@"Updated / ID=" + value.MeasureDataId.ToString() +
						" / Value=" + value.MeasureValue.ToString() +
						" / Explanation=" + value.Explanation +
						" / Action=" + value.Action,
				lastUpdatedOn,
				_user.Id
			);

			measureDataList.Add(
			  new MeasureDataReturnObject {
				  Value = value.MeasureValue,
				  Explanation = value.Explanation,
				  Action = value.Action,
				  Updated = LastUpdatedOnObj(DateTime.Now, _user.UserName)
			  }
			);
			result.Data = measureDataList;
			return result;

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
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	private string BuildRangeString(int? calendarID) {
		int interval = -1;
		var cal = Dbc.Calendar.AsNoTracking().Where(c => c.Id == calendarID);
		if (calendarID is null) {
			interval = (int)Intervals.Daily;
		}
		else {
			interval = cal.Where(c => c.Id == calendarID).Include(c => c.Interval).First().Interval.Id;
		}

		if (interval == (int)Intervals.Daily) {
			var date = cal.First().StartDate ?? new DateTime(0);
			return date.ToString("MMM-dd-yyyy");
		}
		else if (interval == (int)Intervals.Weekly || interval == (int)Intervals.Quarterly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";

		}
		else if (interval == (int)Intervals.Monthly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";
		}
		else if (interval == (int)Intervals.Yearly) {
			var date = cal.First().StartDate ?? new DateTime(0);
			var date2 = cal.First().EndDate ?? new DateTime(0);
			return "[ " + date.ToString("MMM-dd-yyyy") + ", " + date2.ToString("MMM-dd-yyyy") + " ]";
		}
		else {
			throw new Exception(Resource.VAL_INVALID_INTERVAL_ID);
		}
	}
}
