using Deliver.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FilterController : BaseController
{
	[HttpGet]
	public ActionResult<IReadOnlyList<DataImportFilterGetAllResponse>> Get(int intervalId, int year) {
		try {
			var calendarRecords = Dbc.Calendar.Where(c => c.IntervalId == intervalId && c.Year == year)
				.AsNoTrackingWithIdentityResolution();
			return (Intervals)intervalId switch {
				Intervals.Weekly => calendarRecords.Select(c => new DataImportFilterGetAllResponse {
					Id = c.Id,
					Number = c.WeekNumber,
					StartDate = c.StartDate,
					EndDate = c.EndDate,
					Month = null
				}).ToArray(),
				Intervals.Quarterly => calendarRecords.Select(c => new DataImportFilterGetAllResponse {
					Id = c.Id,
					Number = c.Quarter,
					StartDate = c.StartDate,
					EndDate = c.EndDate,
					Month = null
				}).ToArray(),
				Intervals.Monthly => calendarRecords.Select(c => new DataImportFilterGetAllResponse {
					Id = c.Id,
					Number = c.Month,
					StartDate = null,
					EndDate = null,
					Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month ?? 13)
				}).ToArray(),
				_ => BadRequest(Resource.DI_FILTER_INVALID_INTERVAL)
			};
		}
		catch (Exception e) {
			var errorId = LogError(e.Message, e.InnerException, e.StackTrace);
			DataImportFilterGetAllResponse result = new() { Error = new() };
			result.Error.Id = errorId;
			result.Error.Message = e.Message;
			return BadRequest(result);
		}
	}

	private int LogError(string errorMessage, Exception? detailedErrorMessage, string? stacktrace) {
		try {
			var entity = Dbc.ErrorLog.Add(new() {
				ErrorMessage = errorMessage,
				ErrorMessageDetailed = detailedErrorMessage?.ToString() ?? string.Empty,
				StackTrace = stacktrace ?? string.Empty
			}).Entity;
			_ = Dbc.SaveChanges();
			return entity.Id;
		}
		catch {
			return -1;
		}
	}
}
