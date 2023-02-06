using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CLS.WebApi.Controllers.Filters;

[Route("api/filters/[controller]")]
[Authorize]
[ApiController]
public class IntervalsController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public IntervalsController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<JsonResult> Get(MeasureDataFilterReceiveObject values) {
		var returnObject = new IntervalListObject();
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
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
					intervalObject.error.message = Resource.VAL_VALID_INTERVAL_ID;
					returnObject.data.Add(intervalObject);
					break;
			}

			int intervalId = Helper.defaultIntervalId;
			if (values.intervalId != null) {
				intervalId = (int)values.intervalId;
			}

			returnObject.calendarId = _context.Calendar
				.Where(c => c.Interval.Id == intervalId && c.EndDate <= DateTime.Today)
				.OrderByDescending(d => d.EndDate)
				.First().Id;
			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) => "value";

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
