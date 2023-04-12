using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static CLS.WebApi.Helper;

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
			if (intervalId == (int)Intervals.Weekly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					Number = c.WeekNumber,
					StartDate = c.StartDate,
					EndDate = c.EndDate,
					Month = null
				}).ToArray();
			}
			else if (intervalId == (int)Intervals.Quarterly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					Number = c.Quarter,
					StartDate = c.StartDate,
					EndDate = c.EndDate,
					Month = null
				}).ToArray();
			}
			else if (intervalId == (int)Intervals.Monthly) {
				return calendarRecords.Select(c => new DataImportFilterGetAllObject {
					id = c.Id,
					Number = c.Month,
					StartDate = null,
					EndDate = null,
					Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
				}).ToArray();
			}
			else {
				return BadRequest(Resource.DI_FILTER_INVALID_INTERVAL);
			}
		}
		catch (Exception e) {
			var errorId = LogError(e.Message, e.InnerException, e.StackTrace);
			var returnObject = new DataImportFilterGetAllObject { Error = new() };
			returnObject.Error.Id = errorId;
			returnObject.Error.Message = e.Message;
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
