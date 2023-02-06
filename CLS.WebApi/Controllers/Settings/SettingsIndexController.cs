using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CLS.WebApi.Controllers.Settings;

[Route("api/settings/[controller]")]
[Authorize]
[ApiController]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	[HttpGet]
	public ActionResult<JsonResult> Get(SettingsGetRecieveObject value) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.settings, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new SettingsGetReturnObject { locked = new(), users = new(), years = new() };
			var calendarRecords = _context.Calendar.Where(c => c.Year == value.year && c.Interval.Id == (int)Helper.intervals.monthly);
			var settings = _context.Setting;
			if (!settings.Any()) {
				throw new Exception(Resource.SETTINGS_NO_RECORDS);
			}

			var users = from user in _context.User
						where user.Active == true
						select new { id = user.Id, userName = user.UserName };

			var years = _context.Calendar
						.Where(c => c.Interval.Id == (int)Helper.intervals.yearly && c.Year >= DateTime.Now.Year - 3)
						.OrderByDescending(y => y.Year);

			foreach (var yyear in years) {
				returnObject.years.Add(yyear.Year);
			}

			returnObject.year = value.year;
			//returnObject.numberOfDays = settings.First().NumberOfDays;
			var setting = settings.AsNoTracking().First();
			returnObject.active = !setting.Active;
			returnObject.lastCalculatedOn = setting.LastCalculatedOn.ToString();

			returnObject.calculateHH = Helper.CalculateScheduleInt(setting.CalculateSchedule, "HH", ":");
			returnObject.calculateMM = Helper.CalculateScheduleInt(setting.CalculateSchedule, "MM", ":");
			returnObject.calculateSS = Helper.CalculateScheduleInt(setting.CalculateSchedule, "SS", ":");

			foreach (var record in calendarRecords) {
				returnObject.locked.Add(new() {
					id = record.Id,
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Month ?? 13),
					startDate = record.StartDate?.ToString("d"),
					endDate = record.EndDate?.ToString("d"),
					locked = record.Locked
				});
			}

			foreach (var user in users) {
				var currentUser = new UserSettingObject {
					locks = new(),
					id = user.id,
					userName = user.userName
				};
				foreach (var calRecord in calendarRecords) {
					var userCalRecord = _context.UserCalendarLock.Where(c => c.User.Id == user.id && c.Calendar!.Id == calRecord.Id);
					if (userCalRecord == null || userCalRecord.Count() == 0 || userCalRecord.First().LockOverride == false) {
						currentUser.locks.Add(new Lock { lo = false });
					}
					else {
						currentUser.locks.Add(new Lock { lo = true });
					}
				}

				if (user.userName == _config.byPassUserName && _user.userName != _config.byPassUserName) {

				}
				else {
					returnObject.users.Add(currentUser);
				}
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] SettingsGetRecieveObject value) {
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) { throw new Exception(); }

			var lastUpdatedOn = DateTime.Now;
			var returnObject = new SettingsGetReturnObject { locked = new(), years = new() };
			var calendarRecords = _context.Calendar
				.Where(c => c.Year == value.year && c.Interval.Id == (int)Helper.intervals.monthly)
				.OrderBy(c => c.Month);
			var years = _context.Calendar
				.Where(c => c.Interval.Id == (int)Helper.intervals.yearly && c.Year >= DateTime.Now.Year - 3)
				.OrderBy(c => c.Month);
			foreach (var record in calendarRecords.Where(c => c.Month != null)) {
				record.Locked = value.locked.ElementAt((int)record.Month! - 1).locked ?? false;
				record.LastUpdatedOn = lastUpdatedOn;
			}

			var settings = _context.Setting.FirstOrDefault();
			if (settings == null) {
				throw new Exception(Resource.SETTINGS_NO_RECORDS);
			}

			//settings.NumberOfDays = (Int16)value.numberOfDays;
			settings.CalculateSchedule = Helper.CalculateSchedule(value.calculateHH, value.calculateMM, value.calculateSS);
			settings.Active = !value.active;
			settings.LastUpdatedOn = lastUpdatedOn;
			_context.SaveChanges();

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-09",
				Resource.SETTINGS,
				@"Updated",
				lastUpdatedOn,
				_user.userId
			);

			foreach (var yyear in years) {
				returnObject.years.Add(yyear.Year);
			}

			returnObject.year = value.year;
			//returnObject.numberOfDays = settings.NumberOfDays;
			returnObject.active = !settings.Active;
			returnObject.lastCalculatedOn = settings.LastCalculatedOn.ToString();

			returnObject.calculateHH = Helper.CalculateScheduleInt(settings.CalculateSchedule, "HH", ":");
			returnObject.calculateMM = Helper.CalculateScheduleInt(settings.CalculateSchedule, "MM", ":");
			returnObject.calculateSS = Helper.CalculateScheduleInt(settings.CalculateSchedule, "SS", ":");

			foreach (var record in calendarRecords) {
				returnObject.locked.Add(new CalendarLock {
					id = record.Id,
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Month ?? 13),
					startDate = record.StartDate?.ToString("dd/MM/yyyy"),
					endDate = record.EndDate?.ToString("dd/MM/yyyy"),
					locked = record.Locked
				});
			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
