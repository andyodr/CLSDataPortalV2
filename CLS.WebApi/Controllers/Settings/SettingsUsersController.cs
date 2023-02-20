using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public UsersController(ApplicationDbContext context) => _context = context;

	public class Model
	{
		[Required]
		public UserSettingObject Users { get; set; } = null!;

		[Required]
		public int Year { get; set; }
	}

	/// <summary>
	/// Update UserCalendarLock table
	/// </summary>
	/// <param name="model"></param>
	/// <returns></returns>
	[HttpPut]
	public ActionResult<SettingsGetReturnObject> Put(Model model) {
		try {
			var value = new SettingsGetReturnObject { users = new() };
			var user = model.Users;
			value.year = model.Year;
			value.users.Add(user);

			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var calendarRecords = _context.Calendar
				.Where(c => c.Year == model.Year && c.Interval.Id == (int)Helper.intervals.monthly)
				.OrderBy(c => c.Month);

			var lastUpdatedOn = DateTime.Now;
			foreach (var record in calendarRecords) {
				var calendarLock = _context.UserCalendarLock
					.Where(u => u.User.Id == model.Users.Id && u.CalendarId == record.Id);
				if (calendarLock.Any()) {
					var c = calendarLock.First();
					c.LockOverride = value.users.First().Locks?.ElementAt((int)record.Month! - 1).lo;
					c.LastUpdatedOn = lastUpdatedOn;
				}
				else {
					_context.UserCalendarLock.Add(new() {
						Calendar = record,
						LastUpdatedOn = lastUpdatedOn,
						LockOverride = value.users.First().Locks?.ElementAt((int)record.Month! - 1).lo
					}).Property("UserId").CurrentValue = value.users.First().Id;
				}
			}

			_context.SaveChanges();

			Helper.AddAuditTrail(_context,
				Resource.WEB_PAGES,
				"WEB-09",
				Resource.SETTINGS,
				@"Updated Users",
				lastUpdatedOn,
				_user.userId
			);

			return value;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_context, e, _user.userId));
		}
	}
}
