using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CLS.WebApi.Controllers.DataImports;

[Authorize]
[Route("api/[controller]")]
public class FilterController : ControllerBase
{
	private IMeasureTypeRepository _measureTypeRepository;
	private ICalendarRepository _calendarRepository;
	private IErrorLogRepository _errorLogRepository;
	private UserObject _user = new UserObject();

	public FilterController(IMeasureTypeRepository measureTypeRepository, ICalendarRepository calendarRepository, IErrorLogRepository errorLogRepository) {
		_measureTypeRepository = measureTypeRepository;
		_calendarRepository = calendarRepository;
		_errorLogRepository = errorLogRepository;
	}

	// GET: api/values
	[HttpGet]
	public IEnumerable<DataImportFilterGetAllObject> Get(int intervalId, int year) {
		int errorId = 0;
		string errorMessage = string.Empty;
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			var calendarRecords = _calendarRepository.All().Where(c => c.Interval.Id == intervalId && c.Year == year);
			if (intervalId == (int)Helper.intervals.weekly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.WeekNumber,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				});
			}
			else if (intervalId == (int)Helper.intervals.quarterly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Quarter,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				});
			}
			else if (intervalId == (int)Helper.intervals.monthly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Month,
					startDate = null,
					endDate = null,
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName((int)c.Month)
				});
			}
			else throw new Exception(Resource.DI_FILTER_INVALID_INTERVAL);
		}
		catch (Exception e) {
			errorId = _errorLogRepository.logError(e.Message, e.InnerException, e.StackTrace);
			errorMessage = e.Message;
			DataImportFilterGetAllObject returnObject = new DataImportFilterGetAllObject();
			returnObject.error.id = errorId;
			returnObject.error.message = errorMessage;
			return (IEnumerable<DataImportFilterGetAllObject>)returnObject;
		}
	}

}
