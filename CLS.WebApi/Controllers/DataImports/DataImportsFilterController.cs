using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
	public ActionResult<JsonResult> Get(int intervalId, int year) {
		string errorMessage = string.Empty;
		try {
			_user = Helper.UserAuthorization(User);
			if (_user == null) {
				throw new Exception();
			}

			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId)) {
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);
			}

			var calendarRecords = _context.Calendar.Where(c => c.Interval.Id == intervalId && c.Year == year)
				.AsNoTrackingWithIdentityResolution();
			if (intervalId == (int)Helper.intervals.weekly) {
				return new JsonResult(calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.WeekNumber,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				}).ToArray());
			}
			else if (intervalId == (int)Helper.intervals.quarterly) {
				return new JsonResult(calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Quarter,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				}).ToArray());
			}
			else if (intervalId == (int)Helper.intervals.monthly) {
				return new JsonResult(calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Month,
					startDate = null,
					endDate = null,
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
				}).ToArray());
			}
			else {
				throw new Exception(Resource.DI_FILTER_INVALID_INTERVAL);
			}
		}
		catch (Exception e) {
			var errorId = LogError(e.Message, e.InnerException, e.StackTrace);
			errorMessage = e.Message;
			var returnObject = new DataImportFilterGetAllObject();
			returnObject.error.id = errorId;
			returnObject.error.message = errorMessage;
			return new JsonResult(returnObject);
		}
	}

	protected int LogError(string errorMessage, Exception? detailedErrorMessage, string? stacktrace) {
		try {
			var entity = _context.ErrorLog.Add(new() {
				ErrorMessage = errorMessage,
				ErrorMessageDetailed = detailedErrorMessage?.ToString() ?? string.Empty,
				StackTrace = stacktrace ?? string.Empty
			}).Entity;
			_ = _context.SaveChanges();
			return entity.Id;
		}
		catch {
			return -1;
		}
	}
}
