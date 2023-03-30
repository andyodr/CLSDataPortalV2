using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CLS.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public class IndexController : ControllerBase
{
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;
	private UserObject _user = null!;

	public IndexController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	[HttpGet]
	public ActionResult<DataImportsMainObject> Get() {
		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

			var returnObject = new DataImportsMainObject {
				Years = _dbc.Calendar.Where(c => c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsObject { year = c.Year, id = c.Id }).ToArray(),
				//calculationTime = new CalculationTimeObject(),
				CalculationTime = "00:01:00",
				DataImport = new List<DataImportObject>(),
				Intervals = new List<IntervalsObject>(),
				IntervalId = _config.DefaultInterval,
				CalendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _dbc.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.CalculationTime = Helper.CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			// Find Current Year from previuos default interval
			var calendarId = Helper.FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval);
			returnObject.CurrentYear = _dbc.Calendar.Where(c => c.Id == calendarId).First().Year;

			//intervals
			var intervals = _dbc.Interval;
			foreach (var interval in intervals) {
				returnObject.Intervals.Add(new() {
					Id = interval.Id,
					Name = interval.Name
				});
			}

			//dataImport
			DataImportObject measureData = Helper.DataImportHeading(Helper.dataImports.measureData);
			returnObject.DataImport.Add(measureData);

			if (_user.RoleId == (int)Helper.userRoles.systemAdministrator) {
				DataImportObject targetData = Helper.DataImportHeading(Helper.dataImports.target);
				returnObject.DataImport.Add(targetData);

				// This is for kris only
				if (_config.usesCustomer) {
					DataImportObject customerRegionData = Helper.DataImportHeading(Helper.dataImports.customer);
					returnObject.DataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			return BadRequest(Helper.ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
