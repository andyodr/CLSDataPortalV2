using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize(Roles = "SystemAdministrator")]
public sealed class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	/// <summary>
	/// Get settings for selected year
	/// </summary>
	[HttpGet("{year:range(2000,9999)?}")]
	public ActionResult<SettingsGetReturnObject> Get(int? year) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var result = new SettingsGetReturnObject { Locked = new(), Years = new() };
			var calendarRecords = _dbc.Calendar.Where(c => c.Year == (year ?? DateTime.Today.Year) && c.Interval.Id == (int)Intervals.Monthly);
			var settings = _dbc.Setting;
			if (!settings.Any()) {
				return BadRequest(Resource.SETTINGS_NO_RECORDS);
			}

			var years = _dbc.Calendar
						.Where(c => c.Interval.Id == (int)Intervals.Yearly && c.Year >= DateTime.Today.Year - 3)
						.OrderByDescending(y => y.Year);

			foreach (var yyear in years) {
				result.Years.Add(yyear.Year);
			}

			result.Year = year ?? DateTime.Today.Year;
			//returnObject.numberOfDays = settings.First().NumberOfDays;
			var setting = settings.AsNoTracking().First();
			result.Active = !setting.Active;
			result.LastCalculatedOn = setting.LastCalculatedOn.ToString();

			result.CalculateHH = CalculateScheduleInt(setting.CalculateSchedule, "HH", ":");
			result.CalculateMM = CalculateScheduleInt(setting.CalculateSchedule, "MM", ":");
			result.CalculateSS = CalculateScheduleInt(setting.CalculateSchedule, "SS", ":");

			foreach (var record in calendarRecords) {
				result.Locked.Add(new() {
					Id = record.Id,
					Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Month ?? 13),
					StartDate = record.StartDate?.ToString("yyyy-MM-dd"),
					EndDate = record.EndDate?.ToString("yyyy-MM-dd"),
					Locked = record.Locked
				});
			}

			result.Users = _dbc.User
				.Where(u => u.Active == true
					&& (u.UserName != _config.byPassUserName || _user.UserName == _config.byPassUserName))
				.Select(u => new UserSettingObject {
					Id = u.Id,
					UserName = u.UserName,
					Locks = _dbc.Calendar.Where(c => c.Year == result.Year
						&& c.IntervalId == (int)Intervals.Monthly)
						.Select(c => new Lock {
							lo = c.UserCalendarLocks
								.Where(l => l.UserId == u.Id).First().LockOverride ?? false })
						.ToArray()
				})
				.ToList();

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}

	[HttpPut]
	public ActionResult<SettingsGetReturnObject> Put(SettingsGetRecieveObject value) {
		try {
			if (CreateUserObject(User) is not UserObject _user) {
				return Unauthorized();
			}

			var lastUpdatedOn = DateTime.Now;
			var returnObject = new SettingsGetReturnObject { Locked = new(), Years = new() };
			var calendarRecords = _dbc.Calendar
				.Where(c => c.Year == value.Year && c.Interval.Id == (int)Intervals.Monthly)
				.OrderBy(c => c.Month);
			var years = _dbc.Calendar
				.Where(c => c.Interval.Id == (int)Intervals.Yearly && c.Year >= DateTime.Now.Year - 3)
				.OrderBy(c => c.Month);
			foreach (var record in calendarRecords.Where(c => c.Month != null)) {
				record.Locked = value.Locked?.ElementAt((int)record.Month! - 1)?.Locked ?? false;
				record.LastUpdatedOn = lastUpdatedOn;
			}

			var settings = _dbc.Setting.FirstOrDefault();
			if (settings is null) {
				return BadRequest(Resource.SETTINGS_NO_RECORDS);
			}

			//settings.NumberOfDays = (Int16)value.numberOfDays;
			settings.CalculateSchedule = CalculateSchedule(value.CalculateHH, value.CalculateMM, value.CalculateSS);
			settings.Active = !value.Active;
			settings.LastUpdatedOn = lastUpdatedOn;
			_dbc.SaveChanges();

			AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-09",
				Resource.SETTINGS,
				@"Updated",
				lastUpdatedOn,
				_user.Id
			);

			foreach (var yyear in years) {
				returnObject.Years.Add(yyear.Year);
			}

			returnObject.Year = value.Year;
			//returnObject.numberOfDays = settings.NumberOfDays;
			returnObject.Active = !settings.Active;
			returnObject.LastCalculatedOn = settings.LastCalculatedOn.ToString();

			returnObject.CalculateHH = CalculateScheduleInt(settings.CalculateSchedule, "HH", ":");
			returnObject.CalculateMM = CalculateScheduleInt(settings.CalculateSchedule, "MM", ":");
			returnObject.CalculateSS = CalculateScheduleInt(settings.CalculateSchedule, "SS", ":");

			foreach (var record in calendarRecords) {
				returnObject.Locked.Add(new CalendarLock {
					Id = record.Id,
					Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Month ?? 13),
					StartDate = record.StartDate?.ToString("yyyy-MM-dd"),
					EndDate = record.EndDate?.ToString("yyyy-MM-dd"),
					Locked = record.Locked
				});
			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
