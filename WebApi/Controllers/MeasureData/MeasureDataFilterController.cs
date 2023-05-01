using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.MeasureData;

[ApiController]
[Route("api/measuredata/[controller]")]
[Authorize]
public sealed class FilterController : BaseController
{
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FilterReturnObject))]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetIntervalsObject>))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> GetAsync([FromQuery] MeasureDataFilterReceiveObject values, CancellationToken token) {
		if (values.Year is null) {
			return await FilterAsync(token);
		}
		else {
			return await FilterAsync(values, token);
		}
	}

	private async Task<IActionResult> FilterAsync(CancellationToken token) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			FilterReturnObject filter = new() {
				Intervals = await Dbc.Interval.OrderBy(i => i.Id)
					.Select(i => new IntervalsObject { Id = i.Id, Name = i.Name }).ToArrayAsync(token),
				Hierarchy = new RegionFilterObject[] {
					Hierarchy.IndexController.CreateUserHierarchy(Dbc, _user.Id)
				},
				Years = await Dbc.Calendar
					.Where(c => c.Year >= DateTime.Now.Year - 2 && c.Interval.Id == (int)Intervals.Yearly)
					.OrderByDescending(c => c.Year)
					.Select(c => new YearsObject { Id = c.Id, Year = c.Year }).ToArrayAsync(token),
				CurrentCalendarIds = new(),
				MeasureTypes = await Dbc.MeasureType.OrderBy(m => m.Id)
					.Select(m => new MeasureType(m.Id, m.Name, m.Description)).ToArrayAsync(token)
			};

			// set Previous Calendar Ids
			try {
				filter.CurrentCalendarIds.weeklyCalendarId = FindPreviousCalendarId(Dbc.Calendar, (int)Intervals.Weekly);
				filter.CurrentCalendarIds.monthlyCalendarId = FindPreviousCalendarId(Dbc.Calendar, (int)Intervals.Monthly);
				filter.CurrentCalendarIds.quarterlyCalendarId = FindPreviousCalendarId(Dbc.Calendar, (int)Intervals.Quarterly);
				filter.CurrentCalendarIds.yearlyCalendarId = FindPreviousCalendarId(Dbc.Calendar, (int)Intervals.Yearly);
			}
			catch (Exception e) {
				filter.Error = ErrorProcessing(Dbc, e, _user.Id);
			}


			//set filter values
			// Get Previous Calendar Id
			if (_user.savedFilters[Pages.MeasureData].calendarId is null) {
				_user.savedFilters[Pages.MeasureData].calendarId = FindPreviousCalendarId(Dbc.Calendar, Config.DefaultInterval);
			}

			if (_user.savedFilters[Pages.MeasureData].hierarchyId is null) {
				_user.savedFilters[Pages.MeasureData].hierarchyId = 1;
			}

			if (_user.savedFilters[Pages.MeasureData].intervalId is null) {
				_user.savedFilters[Pages.MeasureData].intervalId = Config.DefaultInterval;
			}

			if (_user.savedFilters[Pages.MeasureData].measureTypeId is null) {
				_user.savedFilters[Pages.MeasureData].measureTypeId = Dbc.MeasureType.FirstOrDefault()?.Id;
			}

			//if( _user.savedFilters[Helper.pages.measureData].year == null )
			//  _user.savedFilters[Helper.pages.measureData].year =
			//    _calendarRepository.Find(c => c.IntervalId == (int)Helper.intervals.yearly && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Year;
			if (_user.savedFilters[Pages.MeasureData].year is null) {
				_user.savedFilters[Pages.MeasureData].year = Dbc.Calendar.Find(_user.savedFilters[Pages.MeasureData].calendarId)?.Year;
			}

			filter.Filter = _user.savedFilters[Pages.MeasureData];

			return Ok(filter);
		}
		catch (TaskCanceledException) {
			return StatusCode(499);
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}

	// Used after Measure Data page has been open already.
	private async Task<IActionResult> FilterAsync(MeasureDataFilterReceiveObject body, CancellationToken token) {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			_user.savedFilters[Pages.MeasureData].intervalId = body.IntervalId;

			return body.IntervalId switch {
				(int)Intervals.Weekly => Ok(await Dbc.Calendar.OrderBy(c => c.WeekNumber)
						.Where(c => c.Interval.Id == body.IntervalId && c.Year == body.Year)
						.Select(c => new GetIntervalsObject {
							Id = c.Id,
							Number = c.WeekNumber,
							StartDate = c.StartDate.ToString(),
							EndDate = c.StartDate.ToString(),
							Month = null
						})
						.ToArrayAsync(token)),
				(int)Intervals.Monthly => Ok(await Dbc.Calendar.OrderBy(c => c.Month)
						.Where(c => c.Interval.Id == body.IntervalId && c.Year == body.Year)
						.Select(c => new GetIntervalsObject {
							Id = c.Id,
							Number = c.WeekNumber,
							StartDate = c.StartDate.ToString(),
							EndDate = c.EndDate.ToString(),
							Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
						})
						.ToArrayAsync(token)),
				(int)Intervals.Quarterly => Ok(await Dbc.Calendar.OrderBy(c => c.Quarter)
						.Where(c => c.Interval.Id == body.IntervalId && c.Year == body.Year)
						.Select(d => new GetIntervalsObject {
							Id = d.Id,
							Number = d.WeekNumber,
							StartDate = d.StartDate.ToString(),
							EndDate = d.EndDate.ToString(),
							Month = null
						}).ToArrayAsync(token)),
				_ => Ok(new GetIntervalsObject() {
					Error = new ErrorModel() { Message = Resource.VAL_VALID_INTERVAL_ID }
				})
			};
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(Dbc, e, _user.Id));
		}
	}
}
