using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CLS.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilterController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public FilterController(ApplicationDbContext context) => _context = context;

	[HttpGet]
	public ActionResult<IList<DataImportFilterGetAllObject>> Get(int intervalId, int year) {
		try {
			var calendarRecords = _context.Calendar.Where(c => c.Interval.Id == intervalId && c.Year == year)
				.AsNoTrackingWithIdentityResolution();
			if (intervalId == (int)Helper.intervals.weekly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.WeekNumber,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				}).ToArray();
			}
			else if (intervalId == (int)Helper.intervals.quarterly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Quarter,
					startDate = c.StartDate,
					endDate = c.EndDate,
					month = null
				}).ToArray();
			}
			else if (intervalId == (int)Helper.intervals.monthly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					number = c.Month,
					startDate = null,
					endDate = null,
					month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
				}).ToArray();
			}
			else {
				return BadRequest(Resource.DI_FILTER_INVALID_INTERVAL);
			}
		}
		catch (Exception e) {
			var errorId = LogError(e.Message, e.InnerException, e.StackTrace);
			var returnObject = new DataImportFilterGetAllObject();
			returnObject.error.Id = errorId;
			returnObject.error.Message = e.Message;
			return BadRequest(returnObject);
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
