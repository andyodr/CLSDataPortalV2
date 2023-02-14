using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CLS.WebApi.Controllers.Settings;

[ApiController]
[Route("api/settings/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
	private readonly JsonSerializerOptions webDefaults = new(JsonSerializerDefaults.Web);
	private readonly ApplicationDbContext _context;
	private UserObject _user = null!;

	public UsersController(ApplicationDbContext context) => _context = context;

	[HttpPut]
	public ActionResult<SettingsGetReturnObject> Put([FromBody] string json) {
		try {
			var node = JsonNode.Parse(json);
			var value = new SettingsGetReturnObject { users = new() };
			var test = node!["users"];
			var user = test.Deserialize<UserSettingObject>(webDefaults)!;
			value.year = (int)node["year"]!;
			value.users.Add(user);

			if (Helper.UserAuthorization(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var calendarRecords = _context.Calendar
				.Where(c => c.Year == value.year && c.Interval.Id == (int)Helper.intervals.monthly)
				.OrderBy(c => c.Month);
			//var userLocks = _userRepository.All().Where(u => u.Id == value.users.First().id).First();

			var lastUpdatedOn = DateTime.Now;
			foreach (var record in calendarRecords) {
				var calendarLock = _context.UserCalendarLock
					.Where(u => u.User.Id == value.users.First().Id && u.CalendarId == record.Id);
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
