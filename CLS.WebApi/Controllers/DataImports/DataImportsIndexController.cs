using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.WebApi.Controllers.DataImports;

[Authorize]
[Route("api/dataimports/[controller]")]
public class IndexController : ControllerBase
{
	private IIntervalRepository _intervalRepository;
	private ICalendarRepository _calendarRepository;
	private IAuditTrailRepository _auditTrailRepository;
	private IMeasureDataRepository _measureDataRepository;
	private IMeasureRepository _measureRepository;
	private ISettingRepository _settingRepository;
	private UserObject _user = new UserObject();

	public IndexController(IIntervalRepository intervalRepository
						 , ICalendarRepository calendarRepository
						 , IAuditTrailRepository auditTrailRepository
						 , IMeasureDataRepository measureDataRepository
						 , IMeasureRepository measureRepository
						 , ISettingRepository settingRepository) {
		_intervalRepository = intervalRepository;
		_calendarRepository = calendarRepository;
		_auditTrailRepository = auditTrailRepository;
		_measureDataRepository = measureDataRepository;
		_measureRepository = measureRepository;
		_settingRepository = settingRepository;
	}

	// GET: api/values
	[HttpGet]
	public string Get() {

		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			DataImportsMainObject returnObject = new DataImportsMainObject {
				years = new List<ViewModel.FilterObjects.YearsObject>(),
				//calculationTime = new CalculationTimeObject(),
				calculationTime = "00:01:00",
				dataImport = new List<DataImportObject>(),
				intervals = new List<intervalsObject>()
			};

			returnObject.intervalId = Helper.defaultIntervalId;
			returnObject.calendarId = Helper.FindPreviousCalendarId(_calendarRepository, Helper.defaultIntervalId);

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _settingRepository.All().FirstOrDefault().CalculateSchedule;
			returnObject.calculationTime = Helper.CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			//years
			var years = _calendarRepository.All()
						.Where(c => c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var year in years) {
				returnObject.years.Add(new ViewModel.FilterObjects.YearsObject { year = year.Year, id = year.Id });
			}

			// Find Current Year from previuos default interval
			var calendarId = Helper.FindPreviousCalendarId(_calendarRepository, Helper.defaultIntervalId);
			returnObject.currentYear = _calendarRepository.All().Where(c => c.Id == calendarId).FirstOrDefault().Year;

			//intervals
			var intervals = _intervalRepository.All();
			foreach (var interval in intervals) {
				returnObject.intervals.Add(new intervalsObject {
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
				if (Startup.ConfigurationJson.usesCustomer) {
					DataImportObject customerRegionData = Helper.DataImportHeading(Helper.dataImports.customer);
					returnObject.dataImport.Add(customerRegionData);
				}

			}

			return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
		}
		catch (Exception e) {
			return Helper.errorProcessing(e, _auditTrailRepository, HttpContext, _user);
		}
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
	[HttpPut("{id}")]
	public void Put(int id, [FromBody] string value) {
	}

	// DELETE api/values/5
	[HttpDelete("{id}")]
	public void Delete(int id) {
	}

}
