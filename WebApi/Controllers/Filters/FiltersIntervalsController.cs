using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
	public async Task<ActionResult<IntervalListObject>> GetAsync([FromQuery] MeasureDataFilterReceiveObject values, CancellationToken cancel) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var cal = Dbc.Calendar.Where(c => c.IntervalId == values.IntervalId && c.Year == values.Year);
			int intervalId = values.IntervalId ?? Config.DefaultInterval;
			IntervalListObject result = new() {
				Data = values.IntervalId switch {
					(int)Intervals.Weekly =>
						[..await cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
							Id = d.Id,
							Number = d.WeekNumber,
							StartDate = d.StartDate.ToString(),
							EndDate = d.EndDate.ToString(),
							Month = null
						}).ToArrayAsync(cancel)],
					(int)Intervals.Monthly =>
						[..await cal.OrderBy(c => c.Month).Select(d => new GetIntervalsObject {
							Id = d.Id,
							Number = d.Quarter,
							StartDate = d.StartDate.ToString(),
							EndDate = d.EndDate.ToString(),
							Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(d.Month))
						}).ToArrayAsync(cancel)],
					(int)Intervals.Quarterly =>
						[..await cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
							Id = d.Id,
							Number = d.Quarter,
							StartDate = d.StartDate.ToString(),
							EndDate = d.EndDate.ToString(),
							Month = null
						}).ToArrayAsync(cancel)],
					_ => [new GetIntervalsObject() { Error = new() { Message = Resource.VAL_VALID_INTERVAL_ID } }]
				},
				MeasureTypes = [..await Dbc.MeasureType
					.Where(t => t.MeasureDefinitions!
						.Any(df => df.Measures!
							.Any(m => m.Active == true && m.Hierarchy!.Active == true && m.MeasureData
								.Any(md => md.Calendar!.IntervalId == values.IntervalId && md.Calendar.Year == values.Year))))
					.Select(t => new MeasureType(t.Id, t.Name, t.Description)).ToArrayAsync(cancel)],
				CalendarId = (await Dbc.Calendar
					.Where(c => c.IntervalId == intervalId && c.EndDate <= DateTime.Today)
					.OrderByDescending(d => d.EndDate)
					.FirstOrDefaultAsync(cancel))?.Id ?? -1
			};

			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
