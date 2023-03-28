using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public UsersController(ApplicationDbContext context) => _dbc = context;

	public class Model
	{
		[Required]
		public UserSettingObject User { get; set; } = null!;

		[Required]
		public int Year { get; set; }
	}

	/// <summary>
	/// Update UserCalendarLock table
	/// </summary>
	/// <param name="dto"></param>
	/// <returns></returns>
	[HttpPut]
	public ActionResult<SettingsGetReturnObject> Put(Model dto) {
		try {
			var result = new SettingsGetReturnObject { Year = dto.Year, Users = new() { dto.User } };
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var calendarRecords = _dbc.Calendar
				.Where(c => c.Year == dto.Year && c.Interval.Id == (int)Helper.intervals.monthly)
				.OrderBy(c => c.Month);

			var lastUpdatedOn = DateTime.Now;
			foreach (var record in calendarRecords) {
				var calendarLock = _dbc.UserCalendarLock
					.Where(u => u.User.Id == dto.User.Id && u.CalendarId == record.Id);
				if (calendarLock.Any()) {
					var c = calendarLock.First();
					c.LockOverride = result.Users.First().Locks?.ElementAt((int)record.Month! - 1).lo;
					c.LastUpdatedOn = lastUpdatedOn;
				}
				else {
					_dbc.UserCalendarLock.Add(new() {
						Calendar = record,
						LastUpdatedOn = lastUpdatedOn,
						LockOverride = result.Users.First().Locks?.ElementAt((int)record.Month! - 1).lo
					}).Property("UserId").CurrentValue = result.Users.First().Id;
				}
			}

			_dbc.SaveChanges();

			Helper.AddAuditTrail(_dbc,
				Resource.WEB_PAGES,
				"WEB-09",
				Resource.SETTINGS,
				@"Updated Users",
				lastUpdatedOn,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
