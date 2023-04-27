using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public sealed class IndexController : ControllerBase
{
	private readonly ConfigSettings _config;
	private readonly ApplicationDbContext _dbc;

	public IndexController(IOptions<ConfigSettings> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	[HttpGet]
	public ActionResult<DataImportsMainObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new DataImportsMainObject {
				Years = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsObject { Year = c.Year, Id = c.Id }).ToArray(),
				//calculationTime = new CalculationTimeObject(),
				CalculationTime = "00:01:00",
				DataImport = new List<DataImportObject>(),
				Intervals = _dbc.Interval
                    .Select(i => new IntervalsObject { Id = i.Id, Name = i.Name })
                    .ToArray(),
				IntervalId = _config.DefaultInterval,
				CalendarId = FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _dbc.Setting.First().CalculateSchedule ?? string.Empty;
			result.CalculationTime = CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
								   CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
								   CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			// Find Current Year from previous default interval
			var calendarId = FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval);
			result.CurrentYear = _dbc.Calendar
                .First(c => c.Id == calendarId).Year;

            DataImportObject measureData = DataImportHeading(Helper.DataImports.MeasureData);
			result.DataImport.Add(measureData);

			if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
                DataImportObject targetData = DataImportHeading(Helper.DataImports.Target);
				result.DataImport.Add(targetData);

				// This is for kris only
				if (_config.UsesCustomer) {
                    DataImportObject customerRegionData = DataImportHeading(Helper.DataImports.Customer);
					result.DataImport.Add(customerRegionData);
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(ErrorProcessing(_dbc, e, _user.Id));
		}
	}
}
