using Deliver.WebApi.Data;
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
		var returnObject = new IntervalListObject();
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var cal = Dbc.Calendar.Where(c => c.IntervalId == values.IntervalId && c.Year == values.Year);
			switch (values.IntervalId) {
				case (int)Intervals.Weekly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.WeekNumber,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = null
					}));
					break;
				case (int)Intervals.Monthly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Month).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.WeekNumber,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(d.Month))
					}));
					break;
				case (int)Intervals.Quarterly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						Id = d.Id,
						Number = d.Quarter,
						StartDate = d.StartDate.ToString(),
						EndDate = d.EndDate.ToString(),
						Month = null
					}));
					break;
				default:
					var intervalObject = new GetIntervalsObject();
					intervalObject.Error.Message = Resource.VAL_VALID_INTERVAL_ID;
					returnObject.data.Add(intervalObject);
					break;
			}

			int intervalId = values.IntervalId ?? Config.DefaultInterval;
			returnObject.CalendarId = Dbc.Calendar
				.Where(c => c.IntervalId == intervalId && c.EndDate <= DateTime.Today)
				.OrderByDescending(d => d.EndDate)
				.FirstOrDefault()?.Id ?? -1;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
