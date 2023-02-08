using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CLS.WebApi.Controllers.DataImports;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;
	private UserObject? _user = new();

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public IEnumerable<DataImportFilterGetAllObject> Get(int intervalId, int year) {
		string errorMessage = string.Empty;
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var calendarRecords = _context.Calendar.Where(c => c.Interval.Id == intervalId && c.Year == year);
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
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
				});
			}
			else throw new Exception(Resource.DI_FILTER_INVALID_INTERVAL);
		}
		catch (Exception e) {
			var errorId = LogError(_context, e.Message, e.InnerException, e.StackTrace);
			errorMessage = e.Message;
			var returnObject = new DataImportFilterGetAllObject();
			returnObject.error.id = errorId;
			returnObject.error.message = errorMessage;
			return (IEnumerable<DataImportFilterGetAllObject>)returnObject;
		}
	}

	public int LogError(ApplicationDbContext dbc, string errorMessage, Exception? detailedErrorMessage, string? stacktrace) {
		try {
			var entity = dbc.ErrorLog.Add(new() {
				ErrorMessage = errorMessage,
				ErrorMessageDetailed = detailedErrorMessage?.ToString() ?? string.Empty,
				StackTrace = stacktrace ?? string.Empty
			}).Entity;
			_ = dbc.SaveChanges();
			return entity.Id;
		}
		catch {
			return -1;
		}
	}
}
