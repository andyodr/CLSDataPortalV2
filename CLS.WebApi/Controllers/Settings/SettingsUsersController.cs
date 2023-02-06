using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CLS.WebApi.Controllers.Settings;

[Route("api/settings/[controller]")]
[Authorize]
[ApiController]
public class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public UsersController(ApplicationDbContext context) {
		_context = context;
	}

	// GET: api/values
	[HttpGet]
	public IEnumerable<string> Get() {
		return new string[] { "value1", "value2" };
	}

	// GET api/values/5
	[HttpGet("{id}")]
	public string Get(int id) {
		return "value";
	}

	// POST api/values
	[HttpPost]
	public void Post([FromBody] string value) {
	}

	// PUT api/values/5
	[HttpPut]
	public ActionResult<JsonResult> Put([FromBody] string json) {
		try {
			var node = JsonNode.Parse(json);
			var value = new SettingsGetReturnObject { users = new() };
			var test = node!["users"];
			UserSettingObject user = test.Deserialize<UserSettingObject>()!;
			value.year = (int)node["year"]!;
			value.users.Add(user);

			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			var calendarRecords = _context.Calendar
				.Where(c => c.Year == value.year && c.Interval.Id == (int)Helper.intervals.monthly)
				.OrderBy(c => c.Month);
			//var userLocks = _userRepository.All().Where(u => u.Id == value.users.First().id).First();

			var lastUpdatedOn = DateTime.Now;
			foreach (var record in calendarRecords) {
				var calendarLock = _context.UserCalendarLock
					.Where(u => u.User.Id == value.users.First().id && u.Calendar!.Id == record.Id);
				if (calendarLock.Any()) {
					var c = calendarLock.First();
					c.LockOverride = value.users.First().locks.ElementAt((int)record.Month! - 1).lo;
					c.LastUpdatedOn = lastUpdatedOn;
				}
				else {
					_context.UserCalendarLock.Add(new() {
						Calendar = record,
						LastUpdatedOn = lastUpdatedOn,
						LockOverride = value.users.First().locks.ElementAt((int)record.Month! - 1).lo
					}).Property("UserId").CurrentValue = value.users.First().id;
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

			return new JsonResult(value);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
