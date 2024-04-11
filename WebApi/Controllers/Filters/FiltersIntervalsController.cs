using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Filters;

[ApiController]
[Route("api/filters/[controller]")]
[Authorize]
public sealed class IntervalsController : BaseController
{
	/// <summary>
	/// Get interval data from Calendar table for the specified year
	/// </summary>
	[HttpGet]
	public ActionResult<IntervalListObject> Get([FromQuery] MeasureDataFilterReceiveObject values) {
		IntervalListObject result = new () { Data = [], MeasureTypes = [] };
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var cal = Dbc.Calendar.Where(c => c.IntervalId == values.IntervalId && c.Year == values.Year);
			switch (values.IntervalId) {
				case (int)Intervals.Weekly:
					result.Data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.WeekNumber,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = null
					}));
					break;
				case (int)Intervals.Monthly:
					result.Data.AddRange(cal.OrderBy(c => c.Month).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.Quarter,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(d.Month))
					}));
					break;
				case (int)Intervals.Quarterly:
					result.Data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.Quarter,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = null
					}));
					break;
				default:
                    result.Data.Add(new() { Error = new() { Message = Resource.VAL_VALID_INTERVAL_ID } });
					break;
			}

			int intervalId = values.IntervalId ?? Config.DefaultInterval;
			result.CalendarId = Dbc.Calendar
				.Where(c => c.IntervalId == intervalId && c.EndDate <= DateTime.Today)
				.OrderByDescending(d => d.EndDate)
				.FirstOrDefault()?.Id ?? -1;
			result.MeasureTypes.AddRange(Dbc.MeasureData
				.Where(d => d.Calendar!.IntervalId == values.IntervalId && d.Calendar.Year == values.Year
					&& d.Measure!.Active == true && d.Measure.Hierarchy!.Active == true)
				.Select(d => new MeasureType(d.Measure!.MeasureDefinition!.MeasureType.Id,
					d.Measure.MeasureDefinition.MeasureType.Name,
					d.Measure.MeasureDefinition.MeasureType.Description))
				.Distinct());
			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
