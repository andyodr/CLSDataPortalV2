using Deliver.WebApi.Data;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public sealed class IndexController : BaseController
{
	[HttpGet]
	public ActionResult<DataImportsMainObject> Get() {
		if (CreateUserObject(User) is not UserObject _user) {
			return Unauthorized();
		}

		try {
			var result = new DataImportsMainObject {
				Years = Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsObject { Year = c.Year, Id = c.Id }).ToArray(),
				//calculationTime = new CalculationTimeObject(),
				CalculationTime = "00:01:00",
				DataImport = new List<DataImportObject>(),
				Intervals = Dbc.Interval
                    .Select(i => new IntervalsObject { Id = i.Id, Name = i.Name })
                    .ToArray(),
				IntervalId = Config.DefaultInterval,
				CalendarId = FindPreviousCalendarId(Dbc.Calendar, Config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = Dbc.Setting.First().CalculateSchedule ?? string.Empty;
			result.CalculationTime = CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
								   CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
								   CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			// Find Current Year from previous default interval
			var calendarId = FindPreviousCalendarId(Dbc.Calendar, Config.DefaultInterval);
			result.CurrentYear = Dbc.Calendar
                .First(c => c.Id == calendarId).Year;

            DataImportObject measureData = DataImportHeading(Helper.DataImports.MeasureData);
			result.DataImport.Add(measureData);

			if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
                DataImportObject targetData = DataImportHeading(Helper.DataImports.Target);
				result.DataImport.Add(targetData);

				// This is for kris only
				if (Config.UsesCustomer) {
                    DataImportObject customerRegionData = DataImportHeading(Helper.DataImports.Customer);
					result.DataImport.Add(customerRegionData);
				}
			}

			return result;
		}
		catch (Exception e) {
			return BadRequest(Dbc.ErrorProcessing(e, _user.Id));
		}
	}
}
