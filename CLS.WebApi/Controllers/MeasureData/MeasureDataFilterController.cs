using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CLS.WebApi.Controllers.MeasureData;

[Route("api/measuredata/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = new ();

	public FilterController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public ActionResult<JsonResult> Get(MeasureDataFilterReceiveObject values) {
		if (values.year == null) {
			return new JsonResult(Filter());
		}
		else {
			return new JsonResult(Filter(values));
		}
	}

	// Used first time Measure Data page is open.
	private object Filter() {
		var filter = new FilterReturnObject {
			intervals = new List<IntervalsObject>(),
			measureTypes = new List<MeasureTypeFilterObject>(),
			hierarchy = new List<RegionFilterObject>(),
			years = new List<YearsObject>(),
			currentCalendarIds = new CurrentCalendars()
		};

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.measureData, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			//USE SAVED FILTER
			var intervals = _context.Interval.OrderBy(i => i.Id);
			foreach (var interval in intervals.AsNoTrackingWithIdentityResolution()) {
				filter.intervals.Add(new() { id = interval.Id, name = interval.Name });
			}

			var years = _context.Calendar
						.Where(c => c.Year >= DateTime.Now.Year - 2 && c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var year in years.AsNoTrackingWithIdentityResolution()) {
				YearsObject newYear = new();
				newYear.id = year.Id;
				newYear.year = year.Year;
				filter.years.Add(newYear);
			}

			var measureTypes = _context.MeasureType.OrderBy(m => m.Id);
			foreach (var measureType in measureTypes.AsNoTrackingWithIdentityResolution()) {
				filter.measureTypes.Add(new MeasureTypeFilterObject { Id = measureType.Id, Name = measureType.Name });
			}

			var regions = _context.Hierarchy.AsNoTrackingWithIdentityResolution().Single(m => m.HierarchyLevel!.Id == 1);
			//filter.hierarchy.Add(new RegionFilterObject { hierarchy = regions.Name, id = regions.Id, sub = Helper.getSubs(regions.Id, _user), count = 0 });

			filter.hierarchy.Add(new() {
				hierarchy = regions.Name,
				id = regions.Id,
				found = true,
				sub = Helper.GetSubs(_context, regions.Id, _user),
				count = 0
			});


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
				filter.currentCalendarIds.weeklyCalendarId = Helper.FindPreviousCalendarId(_context.Calendar, (int)Helper.intervals.weekly);
				filter.currentCalendarIds.monthlyCalendarId = Helper.FindPreviousCalendarId(_context.Calendar, (int)Helper.intervals.monthly);
				filter.currentCalendarIds.quarterlyCalendarId = Helper.FindPreviousCalendarId(_context.Calendar, (int)Helper.intervals.quarterly);
				filter.currentCalendarIds.yearlyCalendarId = Helper.FindPreviousCalendarId(_context.Calendar, (int)Helper.intervals.yearly);
			}
			catch { }


			//set filter values
			// Get Previous Calendar Id
			if (_user.savedFilters[Helper.pages.measureData].calendarId == null)
				_user.savedFilters[Helper.pages.measureData].calendarId = Helper.FindPreviousCalendarId(_context.Calendar, Helper.defaultIntervalId);
			if (_user.savedFilters[Helper.pages.measureData].hierarchyId == null)
				_user.savedFilters[Helper.pages.measureData].hierarchyId = 1;
			if (_user.savedFilters[Helper.pages.measureData].intervalId == null)
				_user.savedFilters[Helper.pages.measureData].intervalId = Helper.defaultIntervalId;
			if (_user.savedFilters[Helper.pages.measureData].measureTypeId == null) {
				_user.savedFilters[Helper.pages.measureData].measureTypeId = _context.MeasureType.FirstOrDefault()?.Id;
			}

			//if( _user.savedFilters[Helper.pages.measureData].year == null )
			//  _user.savedFilters[Helper.pages.measureData].year = 
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Year;
			if (_user.savedFilters[Helper.pages.measureData].year == null) {
				_user.savedFilters[Helper.pages.measureData].year = _context.Calendar.Find(_user.savedFilters[Helper.pages.measureData].calendarId)?.Year;
			}

			filter.filter = _user.savedFilters[Helper.pages.measureData];

			return filter;
		}
		catch (Exception e) {
			return Helper.ErrorProcessing(e, _context, HttpContext, _user);
		}
	}

	// Used after Measure Data page has been open already.
	private object Filter(MeasureDataFilterReceiveObject values) {
		var returnObject = new List<GetIntervalsObject>();
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.measureData, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			_user.savedFilters[Helper.pages.measureData].intervalId = values.intervalId;

			if (values.intervalId == (int)Helper.intervals.weekly) {
				var cal = _context.Calendar.OrderBy(c => c.WeekNumber).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
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
				var cal = _context.Calendar.OrderBy(c => c.Month).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
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
				var data = _context.Calendar.OrderBy(c => c.Quarter).Where(c => c.Interval.Id == values.intervalId && c.Year == values.year);
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
				GetIntervalsObject intervalObject = new();
				intervalObject.error.message = Resource.VAL_VALID_INTERVAL_ID;
				returnObject.Add(intervalObject);
			}
			return returnObject;
		}
		catch (Exception e) {
			return Helper.ErrorProcessing(e, _context, HttpContext, _user);
		}
	}
}
