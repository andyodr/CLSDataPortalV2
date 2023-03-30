using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CLS.WebApi.Controllers.MeasureData;

[ApiController]
[Route("api/measuredata/[controller]")]
[Authorize]
public class FilterController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public FilterController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FilterReturnObject))]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetIntervalsObject>))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult Get([FromQuery] MeasureDataFilterReceiveObject values) {
		if (values.year is null) {
			return Filter();
		}
		else {
			return Filter(values);
		}
	}

	// Used first time Measure Data page is open.
	private IActionResult Filter() {
		var filter = new FilterReturnObject {
			Intervals = new(),
			Hierarchy = new(),
			Years = new(),
			CurrentCalendarIds = new()
		};

		try {
			if (Helper.CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			//USE SAVED FILTER
			var intervals = _dbc.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals.AsNoTrackingWithIdentityResolution()) {
				filter.Intervals.Add(new() { Id = interval.Id, Name = interval.Name });
			}

			var cals = _dbc.Calendar
						.Where(c => c.Year >= DateTime.Now.Year - 2 && c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var cal in cals.AsNoTrackingWithIdentityResolution()) {
				filter.Years.Add(new YearsObject { id = cal.Id, year = cal.Year });
			}

			filter.MeasureTypes = _dbc.MeasureType.OrderBy(m => m.Id)
				.Select(m => new MeasureTypeFilterObject { Id = m.Id, Name = m.Name })
				.ToArray();
			filter.Hierarchy.Add(Hierarchy.IndexController.CreateUserHierarchy(_dbc, _user.Id));

			// set current Calendar Ids
			//try
			//{
			//  filter.currentCalendarIds.weeklyCalendarId =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.weekly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Id;

			//  filter.currentCalendarIds.monthlyCalendarId =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.monthly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Id;

			// filter.currentCalendarIds.quarterlyCalendarId =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.quarterly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Id;

			// filter.currentCalendarIds.yearlyCalendarId =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Id;
			//}
			//catch {}

			// set Previous Calendar Ids
			try {
				filter.CurrentCalendarIds.weeklyCalendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, (int)Helper.intervals.weekly);
				filter.CurrentCalendarIds.monthlyCalendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, (int)Helper.intervals.monthly);
				filter.CurrentCalendarIds.quarterlyCalendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, (int)Helper.intervals.quarterly);
				filter.CurrentCalendarIds.yearlyCalendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, (int)Helper.intervals.yearly);
			}
			catch (Exception e) {
				filter.Error = Helper.ErrorProcessing(_dbc, e, _user.Id);
			}


			//set filter values
			// Get Previous Calendar Id
			if (_user.savedFilters[Helper.pages.measureData].calendarId is null) {
				_user.savedFilters[Helper.pages.measureData].calendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval);
			}

			if (_user.savedFilters[Helper.pages.measureData].hierarchyId is null) {
				_user.savedFilters[Helper.pages.measureData].hierarchyId = 1;
			}

			if (_user.savedFilters[Helper.pages.measureData].intervalId is null) {
				_user.savedFilters[Helper.pages.measureData].intervalId = _config.DefaultInterval;
			}

			if (_user.savedFilters[Helper.pages.measureData].measureTypeId is null) {
				_user.savedFilters[Helper.pages.measureData].measureTypeId = _dbc.MeasureType.FirstOrDefault()?.Id;
			}

			//if( _user.savedFilters[Helper.pages.measureData].year == null )
			//  _user.savedFilters[Helper.pages.measureData].year =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Year;
			if (_user.savedFilters[Helper.pages.measureData].year is null) {
				_user.savedFilters[Helper.pages.measureData].year = _dbc.Calendar.Find(_user.savedFilters[Helper.pages.measureData].calendarId)?.Year;
			}

			filter.Filter = _user.savedFilters[Helper.pages.measureData];

			return Ok(filter);
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	// Used after Measure Data page has been open already.
	private IActionResult Filter(MeasureDataFilterReceiveObject values) {
		var returnObject = new List<GetIntervalsObject>();
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			_user.savedFilters[Helper.pages.measureData].intervalId = values.intervalId;

			if (values.intervalId == (int)Helper.intervals.weekly) {
				var cal = _dbc.Calendar.OrderBy(c => c.WeekNumber).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
				foreach (var c in cal.AsNoTracking()) {
					returnObject.Add(new GetIntervalsObject {
						id = c.Id,
						number = c.WeekNumber,
						startDate = c.StartDate.ToString(),
						endDate = c.StartDate.ToString(),
						month = null
					});
				}
			}
			else if (values.intervalId == (int)Helper.intervals.monthly) {
				var cal = _dbc.Calendar.OrderBy(c => c.Month).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
				foreach (var c in cal.AsNoTracking()) {
					returnObject.Add(new GetIntervalsObject {
						id = c.Id,
						number = c.WeekNumber,
						startDate = c.StartDate.ToString(),
						endDate = c.EndDate.ToString(),
						month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
					});
				}
			}
			else if (values.intervalId == (int)Helper.intervals.quarterly) {
				var data = _dbc.Calendar.OrderBy(c => c.Quarter).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
				var dataObject = data.Select(d => new GetIntervalsObject {
					id = d.Id,
					number = d.WeekNumber,
					startDate = d.StartDate.ToString(),
					endDate = d.EndDate.ToString(),
					month = null
				});
				foreach (var Object in dataObject) {
					returnObject.Add(new GetIntervalsObject { id = Object.id, number = Object.number, startDate = Object.startDate, endDate = Object.endDate, month = Object.month });
				}
			}
			else {
				var intervalObject = new GetIntervalsObject();
				intervalObject.error.Message = Resource.VAL_VALID_INTERVAL_ID;
				returnObject.Add(intervalObject);
			}
			return Ok(returnObject);
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
