using Deliver.WebApi.Data;
using Deliver.WebApi.Data.Models;
using Deliver.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public sealed class UploadController : BaseController
{
	private readonly JsonSerializerOptions webDefaults = new(JsonSerializerDefaults.Web);
	private readonly DataImportsUploadResponse result = new() { Data = new(), Error = [] };
	private UserDto _user = new();

	public sealed class Model
	{
		[Required]
		public int DataImport { get; set; }

		[Required]
		public string Sheet { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<ImportTarget>> Data { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<ImportTarget>> Targets { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataCustomer>> Customers { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataMeasureData>> MeasureData { get; set; } = null!;

		[Required]
		public int CalendarId { get; set; }
	}

	[HttpPost]
	public ActionResult<DataImportsUploadResponse> Post([FromBody] dynamic jsonString) {
		int rowNumber = 1;
		if (CreateUserObject(User) is not UserDto user) {
			return Unauthorized();
		}

		_user = user;
		try {
			string json = jsonString.ToString();
			json = Regex.Replace(
				json,
				Regex.Escape("Hierarchy ID"),
				"hierarchyId".Replace("$", "$$"),
				RegexOptions.IgnoreCase
			);
			json = Regex.Replace(
				json,
				Regex.Escape("Measure ID"),
				"measureId".Replace("$", "$$"),
				RegexOptions.IgnoreCase
			);
			json = Regex.Replace(
				json,
				Regex.Escape("measureId"),
				"measureDefinitionId".Replace("$", "$$"),
				RegexOptions.IgnoreCase
			);

			var jsonObject = JsonNode.Parse(json);
			var dataImport = (Helper.DataImports)int.Parse(jsonObject!["dataImport"]?.ToString() ?? "0");
			string sheetName = jsonObject["sheet"]?.ToString() ?? string.Empty;
			var array = jsonObject["data"] as JsonArray;

			switch (dataImport) {
				case Helper.DataImports.Target:
					List<ImportTarget> impTargets = [];
					foreach (var token in array!) {
						var targetData = token.Deserialize<ImportTarget>(webDefaults)!;
						targetData.RowNumber = rowNumber++;
						var df = Dbc.MeasureDefinition.Where(md => md.Id == targetData.MeasureDefinitionId).FirstOrDefault();
						if (df is not null) {
							targetData.Precision = df.Precision;
							impTargets.Add(targetData);
						}
						else {
							result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_TARGET_NO_EXIST });
						}
					}

					if (result.Error.Count != 0) {
						result.Error = [.. result.Error.OrderBy(e => e.Row)];
						return result;
					}

					// Find duplicates
					var duplicates = from t in impTargets
									 group t by new { t.HierarchyId, t.MeasureDefinitionId } into g
									 where g.Count() > 1
									 select g.Key;
					if (duplicates.Any()) {
						var duplicateRows = from t in impTargets
											join d in duplicates
											on new { a = t.HierarchyId, b = t.MeasureDefinitionId }
											equals new { a = d.HierarchyId, b = d.MeasureDefinitionId }
											select t.RowNumber;

						foreach (var itemRow in duplicateRows) {
							result.Error.Add(new() { Row = itemRow, Message = Resource.DI_ERR_TARGET_REPEATED });
						}

						result.Error = [.. result.Error.OrderBy(e => e.Row)];
						return result;
					}

					foreach (var target in impTargets) {
						if (Dbc.ValidateTargetImport(target, _user.Id) is ImportErrorResult err) {
							result.Error.Add(err);
						};
					}

					if (result.Error.Count != 0) {
						result.Error = [.. result.Error.OrderBy(e => e.Row)];
					}
					else {
						foreach (var target in impTargets) {
							if (Dbc.ImportTarget(target, _user.Id) is ImportErrorResult err) {
								result.Error.Add(err);
							}
						}

						result.Data = null;
						Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-06",
							Resource.DATA_IMPORT,
							@"Target Imported" + " / sheetName=" + sheetName,
							DateTime.Now,
							_user.Id
						);
					}

					return result;
				case Helper.DataImports.Customer when Config.UsesCustomer:
					var listCustomer = new List<SheetDataCustomer>();
					foreach (var token in array!) {
						var customerData = token.Deserialize<SheetDataCustomer>(webDefaults);
						if (customerData == null) {
							continue;
						}

						customerData.RowNumber = rowNumber++;
						ValidateCustomerRows(customerData, _user.Id);
						listCustomer.Add(customerData);
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = [.. temp];
					}
					else {
						foreach (var row in listCustomer) {
							ImportCustomerRecords(row);
						}

						result.Data = null;

						Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-06",
							Resource.DATA_IMPORT,
							@"Customer Imported" + " / sheetName=" + sheetName,
							DateTime.Now,
							_user.Id
						);
					}

					return result;
				case Helper.DataImports.MeasureData:
					JsonNode? calId = jsonObject["calendarId"];
					int calendarId = int.Parse(calId!.ToString());
					Calendar? calendar = Dbc.Calendar.Where(c => c.Id == calendarId).First();
					List<SheetDataMeasureData> listMeasureData = [];

					// From settings page, DO NOT USE = !Active
					if (Dbc.Setting.First().Active == true) {
						if (Dbc.IsDataLocked(calendar.IntervalId, _user.Id, calendar)) {
							throw new Exception(Resource.DI_ERR_USER_DATE);
						}
					}

					foreach (var node in array!) {
						var measureData = node.Deserialize<SheetDataMeasureData>(webDefaults);
						if (measureData is null) {
							continue;
						}

						measureData.RowNumber = rowNumber++;
						var mdef = Dbc.MeasureDefinition.Where(md => md.Id == measureData.MeasureDefinitionId).FirstOrDefault();
						if (mdef is Data.Models.MeasureDefinition md) {
							measureData.UnitId = md.UnitId;
							measureData.Precision = md.Precision;
							listMeasureData.Add(measureData);
						}
						else {
							result.Error.Add(new() { Row = measureData.RowNumber, Message = Resource.DI_ERR_NO_MEASURE });
						}
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = [.. temp];
						return result;
					}

					foreach (var row in listMeasureData) {
						ValidateMeasureData(row, calendar.IntervalId, calendar.Id, _user.Id);
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = [.. temp];
					}
					else {
						foreach (var row in listMeasureData) {
							ImportMeasureDataRecords(row, calendarId, _user.Id);
						}

						result.Data = null;

						Dbc.AddAuditTrail(Resource.WEB_PAGES, "WEB-06",
							Resource.DATA_IMPORT,
							@"Measure Data Imported" +
								" / CalendarId=" + calendar.Id.ToString() +
								" / Interval=" + Dbc.Interval.Where(i => i.Id == calendar.IntervalId).First().Name +
								" / Year=" + calendar.Year.ToString() +
								" / Quarter=" + calendar.Quarter.ToString() +
								" / Month=" + calendar.Month.ToString() +
								" / Week=" + calendar.WeekNumber.ToString() +
								" / sheetName=" + sheetName,
							DateTime.Now,
							_user.Id
						);
					}

					return result;
				default:
					return result;
			}
		}
		catch (Exception e) {
			var record = Dbc.AuditTrail.Add(new AuditTrail {
				UpdatedBy = _user!.Id,
				Type = Resource.WEB_PAGES,
				Code = "WEB-06",
				Data = e.Message + "\n" + (e.StackTrace?.ToString() ?? string.Empty),
				Description = Resource.DATA_IMPORT,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			Dbc.SaveChanges();
			return new DataImportsUploadResponse { Error = new() { new() { Id = record.Id, Row = null, Message = e.Message } } };
		}
	}

	private DataImportsResponse? DataReturn(UserDto user) {
		try {
			var returnObject = new DataImportsResponse {
				Years = [.. Dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsDto { Year = c.Year, Id = c.Id })],
				CalculationTime = "00:01:00",
				DataImport = [DataImportHeading(Helper.DataImports.MeasureData)],
				Intervals = [.. Dbc.Interval.Select(i => new IntervalDto { Id = i.Id, Name = i.Name })],
				IntervalId = Config.DefaultInterval,
				CalendarId = FindPreviousCalendarId(Dbc.Calendar, Config.DefaultInterval)
			};

			string sCalculationTime = Dbc.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.CalculationTime = CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
				returnObject.DataImport.Add(DataImportHeading(Helper.DataImports.Target));

				if (Config.UsesCustomer) {
					DataImportsResponseDataImportElement customerRegionData = DataImportHeading(Helper.DataImports.Customer);
					returnObject.DataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			Dbc.ErrorProcessing(e, _user.Id);
			return null;
		}
	}

	// --------------------------------------------------------
	// MeasureData
	// --------------------------------------------------------
	private void ValidateMeasureData(SheetDataMeasureData row, int intervalId, int calendarId, int userId) {
		//check for null values
		if (row.MeasureDefinitionId is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_MEASURE_NULL });
			return;
		}

		if (row.HierarchyId is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL });
			return;
		}

		//check userHierarchy
		if (Dbc.IsHierarchyValidated(row.RowNumber, row.HierarchyId ?? -1, row.Value, userId) is ImportErrorResult err) {
			result.Error.Add(err);
		};

		//check measureData
		if (!Dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyId
				&& md.Measure.MeasureDefinitionId == row.MeasureDefinitionId
				&& md.CalendarId == calendarId).Any()) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE_DATA });
		}

		//check Measure Definition percentage Unit
		if (row.Value is double v && row.UnitId == 1 && (v < 0 || v > 1)) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.VAL_VALUE_UNIT });
		}

		//check Measure
		if (Dbc.Measure.Where(m => m.Active == true && m.HierarchyId == row.HierarchyId
			&& m.MeasureDefinitionId == row.MeasureDefinitionId)
			.Include(m => m.MeasureDefinition)
			.AsNoTrackingWithIdentityResolution()
			.FirstOrDefault() is Measure m) {
			MeasureCalculatedDto measureCalculated = new() {
				ReportIntervalId = m.MeasureDefinition!.ReportIntervalId,
				Calculated = m.MeasureDefinition.Calculated ?? false,
				AggDaily = m.MeasureDefinition.AggDaily ?? false,
				AggWeekly = m.MeasureDefinition.AggWeekly ?? false,
				AggMonthly = m.MeasureDefinition.AggMonthly ?? false,
				AggQuarterly = m.MeasureDefinition.AggQuarterly ?? false,
				AggYearly = m.MeasureDefinition.AggYearly ?? false
			};
			var bMdExpression = m.Expression ?? false;
			var hId = m.HierarchyId;
			if (row.Value is not null &&
				Dbc.IsMeasureCalculated(bMdExpression, hId, intervalId, row.MeasureDefinitionId ?? -1, measureCalculated)) {
				result.Error.Add(new() {
					Row = row.RowNumber,
					Message = Resource.DI_ERR_CALCULATED
				});
			}
		}
		else {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE });
		}
	}

	private void ImportMeasureDataRecords(SheetDataMeasureData row, int calendarId, int userId) {
		try {
			double? sheetValue = row.Value switch {
				double value => Math.Round(value, row.Precision, MidpointRounding.AwayFromZero),
				_ => null
			};

			_ = Dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyId
				&& md.Measure.MeasureDefinitionId == row.MeasureDefinitionId && md.CalendarId == calendarId)
				.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, (byte)IsProcessed.MeasureData)
					.SetProperty(md => md.UserId, userId)
					.SetProperty(md => md.LastUpdatedOn, DateTime.Now)
					.SetProperty(md => md.Value, md => sheetValue ?? md.Value)
					.SetProperty(md => md.Explanation, md => row.Explanation ?? md.Explanation)
					.SetProperty(md => md.Action, md => row.Action ?? md.Action));
		}
		catch {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_UPLOADING });
		}
	}

	// --------------------------------------------------------
	// Customer Hierarchy
	// --------------------------------------------------------
	private void ValidateCustomerRows(SheetDataCustomer row, int userId) {
		if (row.HierarchyId is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL });
			return;
		}

		if (row.CalendarId is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_CALENDAR_NULL });
			return;
		}

		if(Dbc.IsHierarchyValidated(row.RowNumber, (int)row.HierarchyId, null, userId) is ImportErrorResult err) {
			result.Error.Add(err);
		};

		if (Dbc.Calendar.Find(row.CalendarId) is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_CALENDAR_NO_EXIST });
		}
	}

	private void ImportCustomerRecords(SheetDataCustomer row) {
		try {
			_ = Dbc.CustomerRegion.Add(new() {
				HierarchyId = row.HierarchyId ?? -1,
				CalendarId = row.CalendarId ?? -1,
				CustomerGroup = row.CustomerGroup,
				CustomerSubGroup = row.CustomerSubGroup,
				PurchaseType = row.PurchaseType,
				TradeChannel = row.TradeChannel,
				TradeChannelGroup = row.TradeChannelGroup,
				Sales = row.Sales,
				NumOrders = row.NumOrders,
				NumLines = row.NumLines,
				OrderType = row.OrderType,
				NumLateOrders = row.NumLateOrders,
				NumLateLines = row.NumLateLines,
				NumOrdLens = row.NumOrdLens,
				OrdQty = row.OrdQty,
				IsProcessed = (byte)IsProcessed.Complete,
				HeaderStatusCode = row.HeaderStatusCode,
				HeaderStatus = row.HeaderStatus,
				BlockCode = row.BlockCode,
				BlockText = row.BlockText,
				RejectionCode = row.RejectionCode,
				RejectionText = row.RejectionText,
				CreditStatusCheck = row.CreditStatusCheck,
				CreditCode = row.CreditCode
			});
			_ = Dbc.SaveChanges();
		}
		catch {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_UPLOADING });
		}
	}
}
