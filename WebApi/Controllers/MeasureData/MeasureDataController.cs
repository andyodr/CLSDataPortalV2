using Deliver.WebApi.Data;
using Deliver.WebApi.Data.Models;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureData;

public record VariableName(long Id, string VarName);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MeasureDataController : BaseController
{
	/// <summary>
	/// Get a MeasureDataIndexListObject
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<MeasureDataIndexResponse>> GetAsync([FromQuery] MeasureDataRequest dto, CancellationToken cancel) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			MeasureDataIndexResponse result = new() {
				Data = [],
				CalendarId = dto.CalendarId,
				Range = BuildRangeString(dto.CalendarId),
				Locked = false,
				Allow = await Dbc.UserHierarchy
					.Where(d => d.UserId == _user.Id && d.HierarchyId == dto.HierarchyId).AnyAsync(cancel),
				EditValue = CanEditValueFromSpecialHierarchy(Config, dto.HierarchyId)
			};

			DateTime? date = string.IsNullOrEmpty(dto.Day) ? null : Convert.ToDateTime(dto.Day);
			var cal = await Dbc.Calendar
				.Where(c => c.StartDate == date)
				.AsNoTracking().ToArrayAsync(cancel);
			if (cal.Length > 0) {
				result.CalendarId = cal.First().Id;
			}

			var calendar = await Dbc.Calendar
				.Where(c => c.Id == result.CalendarId)
				.AsNoTracking().FirstAsync(cancel);

			// From settings page, DO NOT USE = !Active
			if ((await Dbc.Setting.AsNoTracking().FirstAsync(cancel)).Active == true) {
				result.Locked = Dbc.IsDataLocked(calendar.IntervalId, _user.Id, calendar);
			}

			var measureData = await Dbc.MeasureData
				.Where(m => m.Measure!.Active == true
					&& m.Measure.HierarchyId == dto.HierarchyId
					&& m.Measure.MeasureDefinition!.MeasureTypeId == dto.MeasureTypeId
					&& m.CalendarId == result.CalendarId)
				.OrderBy(m => m.Measure!.MeasureDefinition!.FieldNumber)
				.ThenBy(m => m.Measure!.MeasureDefinition!.Priority)
				.ThenBy(m => m.Measure!.MeasureDefinition!.Name)
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
					md.Measure.MeasureDefinition.VariableName,
					md.Explanation,
					md.Action,
					md.Measure.HierarchyId,
					calculated = Dbc.IsMeasureCalculated(md.Measure.Expression ?? false, dto.HierarchyId, calendar.IntervalId, md.Measure.MeasureDefinition.Id, null)
				})
				.ToArrayAsync(cancel);

			var variables = await Dbc.MeasureData
				.Where(d => d.CalendarId == result.CalendarId
					&& d.Measure!.HierarchyId == dto.HierarchyId
					&& d.Measure!.Active == true
					&& d.Measure!.MeasureDefinition!.MeasureTypeId == dto.MeasureTypeId)
				.Select(d => new { d.Measure!.MeasureDefinitionId, d.Measure.MeasureDefinition!.VariableName, d.Value })
				.ToArrayAsync(cancel);

			foreach (var md in measureData) {
				MeasureDataResponse measureDataDto = new() {
					Id = md.Id,
					Name = md.Name,
					Description = md.Description,
					Explanation = md.Explanation,
					Action = md.Action,
					UnitId = md.Unit.Id,
					Units = md.Unit.Short,
					Target = md.target is double target ? Math.Round(target, md.Precision, MidpointRounding.AwayFromZero) : null,
					Yellow = md.YellowValue is double yellow ? Math.Round(yellow, md.Precision, MidpointRounding.AwayFromZero) : null,
					Value = md.Value is double value ? Math.Round(value, md.Precision, MidpointRounding.AwayFromZero) : null,
					Calculated = md.calculated,
					VariableName = md.VariableName,
					Expression = md.Expression,
					Evaluated = String.Empty,
					Updated = LastUpdatedOnObj(md.lastUpdatedOn, md.User?.UserName ?? Resource.SYSTEM)
				};

				if (measureDataDto.Calculated && !string.IsNullOrEmpty(measureDataDto.Expression)) {
					// Search measure data values from variables
					string sExpression = measureDataDto.Expression;
					foreach (var variable in variables) {
						if (variable.Value is double v) {
							sExpression = sExpression.Replace($@"Data[""{variable.VariableName}""]", v.ToString());
						}
					}

					measureDataDto.Evaluated = sExpression;
				}

				result.Data.Add(measureDataDto);
			}

			if (dto.CalendarId != _user.savedFilters[Pages.MeasureData].calendarId) {
				_user.savedFilters[Pages.MeasureData].calendarId = dto.CalendarId;
				_user.savedFilters[Pages.MeasureData].intervalId = calendar.IntervalId;
				_user.savedFilters[Pages.MeasureData].year = calendar.Year;
			}
			_user.savedFilters[Pages.MeasureData].hierarchyId = dto.HierarchyId;
			result.Filter = _user.savedFilters[Pages.MeasureData];
			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<MeasureDataIndexResponse> Put(MeasureDataRequest value) {
		MeasureDataIndexResponse result = new();
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			double? measureValue = null;
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
				measureValue = Math.Round((double)value.MeasureValue, mData.Precision, MidpointRounding.AwayFromZero);
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

			Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-01",
				Resource.MEASURE_DATA,
				@"Updated / ID=" + value.MeasureDataId.ToString() +
						" / Value=" + measureValue.ToString() +
						" / Explanation=" + value.Explanation +
						" / Action=" + value.Action,
				lastUpdatedOn,
				_user.Id
			);

			result.Data = [new() {
				  Value = measureValue,
				  Explanation = value.Explanation,
				  Action = value.Action,
				  Updated = LastUpdatedOnObj(DateTime.Now, _user.UserName)
			  }];
			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}

	/// <returns>Interval range string for display above the table</returns>
	private string BuildRangeString(int? calendarId) {
		Calendar calendar = Dbc.Calendar.Where(c => c.Id == calendarId).AsNoTracking()
			.FirstOrDefault() ?? new() { IntervalId = (int)Intervals.Daily };

		var date1 = (calendar.StartDate ?? new DateTime(0)).ToString("MMM-dd-yyyy");
		var date2 = (calendar.EndDate ?? new DateTime(0)).ToString("MMM-dd-yyyy");
		return (Intervals)calendar.IntervalId switch {
			Intervals.Weekly => $"[{calendar.WeekNumber}: {date1}, {date2} ]",
			Intervals.Quarterly => $"[Q{calendar.Quarter}: {date1}, {date2} ]",
			Intervals.Monthly or Intervals.Yearly => $"[ {date1}, {date2} ]",
			_ => date1
		};
	}

	/// <summary>
	/// This is for a special case where some level 2 hierarchies can not be edited since they are a sum value
	/// </summary>
	internal static bool CanEditValueFromSpecialHierarchy(ConfigSettings config, int hierarchyId) =>
		!(config.SpecialHierarchies is List<int> hierarchies && hierarchies.Contains(hierarchyId));
}
