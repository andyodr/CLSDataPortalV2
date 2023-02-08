using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.DataImports;

[Route("api/dataimports/[controller]")]
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
	public ActionResult<JsonResult> Get() {

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var returnObject = new DataImportsMainObject {
				years = new(),
				//calculationTime = new CalculationTimeObject(),
				calculationTime = "00:01:00",
				dataImport = new List<DataImportObject>(),
				intervals = new List<IntervalsObject>(),
				intervalId = Helper.defaultIntervalId,
				calendarId = Helper.FindPreviousCalendarId(_context.Calendar, Helper.defaultIntervalId)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _context.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.calculationTime = Helper.CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			//years
			var years = _context.Calendar
						.Where(c => c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var year in years) {
				returnObject.years.Add(new() { year = year.Year, id = year.Id });
			}

			// Find Current Year from previuos default interval
			var calendarId = Helper.FindPreviousCalendarId(_context.Calendar, Helper.defaultIntervalId);
			returnObject.currentYear = _context.Calendar.Where(c => c.Id == calendarId).First().Year;

			//intervals
			var intervals = _context.Interval;
			foreach (var interval in intervals) {
				returnObject.intervals.Add(new IntervalsObject {
					id = interval.Id,
					name = interval.Name
				});
			}

			//dataImport
			DataImportObject measureData = Helper.DataImportHeading(Helper.dataImports.measureData);
			returnObject.dataImport.Add(measureData);

			if (_user.userRoleId == (int)Helper.userRoles.systemAdministrator) {
				DataImportObject targetData = Helper.DataImportHeading(Helper.dataImports.target);
				returnObject.dataImport.Add(targetData);

				// This is for kris only
				if (_config.usesCustomer) {
					DataImportObject customerRegionData = Helper.DataImportHeading(Helper.dataImports.customer);
					returnObject.dataImport.Add(customerRegionData);
				}

			}

			return new JsonResult(returnObject);
		}
		catch (Exception e) {
			return new JsonResult(Helper.ErrorProcessing(e, _context, HttpContext, _user));
		}
	}

	[HttpGet("{id}")]
	public string Get(int id) => "value";

	[HttpPost]
	public void Post([FromBody] string value) {
	}

	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	[HttpDelete("{id}")]
	public void Delete(int id) {
	}
}
