using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CLS.WebApi.Controllers.Filters;

[ApiController]
[Route("api/filters/[controller]")]
[Authorize]
public class IntervalsController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public IntervalsController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public ActionResult<IntervalListObject> Get(MeasureDataFilterReceiveObject values) {
		var returnObject = new IntervalListObject();
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var cal = _context.Calendar.Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
			switch (values.intervalId) {
				case (int)Helper.intervals.weekly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						id = d.Id,
						number = d.WeekNumber,
						startDate = d.StartDate.ToString(),
						endDate = d.EndDate.ToString(),
						month = null
					}));
					break;
				case (int)Helper.intervals.monthly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Month).Select(d => new GetIntervalsObject {
						id = d.Id,
						number = d.WeekNumber,
						startDate = d.StartDate.ToString(),
						endDate = d.EndDate.ToString(),
						month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Convert.ToInt16(d.Month))
					}));
					break;
				case (int)Helper.intervals.quarterly:
					returnObject.data.AddRange(cal.OrderBy(c => c.Quarter).Select(d => new GetIntervalsObject {
						id = d.Id,
						number = d.Quarter,
						startDate = d.StartDate.ToString(),
						endDate = d.EndDate.ToString(),
						month = null
					}));
					break;
				default:
					var intervalObject = new GetIntervalsObject();
					intervalObject.error.Message = Resource.VAL_VALID_INTERVAL_ID;
					returnObject.data.Add(intervalObject);
					break;
			}

			int intervalId = values.intervalId ?? _config.DefaultInterval;
			returnObject.calendarId = _context.Calendar
				.Where(c => c.Interval.Id == intervalId && c.EndDate <= DateTime.Today)
				.OrderByDescending(d => d.EndDate)
				.First().Id;
			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
