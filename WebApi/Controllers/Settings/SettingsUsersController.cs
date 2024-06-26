using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _dbc;

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
	[HttpPut]
	public ActionResult<SettingsGetResponse> Put(Model dto) {
		if (CreateUserObject(User) is not UserDto _user) {
			return Unauthorized();
		}

		try {
			var result = new SettingsGetResponse { Year = dto.Year, Users = [dto.User] };
			var calendarRecords = _dbc.Calendar
				.Where(c => c.Year == dto.Year && c.IntervalId == (int)Intervals.Monthly)
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

			_dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-09",
				Resource.SETTINGS,
				@"Updated Users",
				lastUpdatedOn,
				_user.Id
			);

			return result;
		}
		catch (Exception e) {
			return BadRequest(_dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
