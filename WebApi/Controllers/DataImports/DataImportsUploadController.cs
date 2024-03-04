using Deliver.WebApi.Data;
using Deliver.WebApi.Data.Models;
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
	private readonly DataImportReturnObject result = new() { Data = new(), Error = new() };
	private UserObject _user = new();

	public sealed class Model
	{
		[Required]
		public int DataImport { get; set; }

		[Required]
		public string Sheet { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataTarget>> Data { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataTarget>> Targets { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataCustomer>> Customers { get; set; } = null!;

		public IReadOnlyDictionary<string, IList<SheetDataMeasureData>> MeasureData { get; set; } = null!;

		[Required]
		public int CalendarId { get; set; }
	}

	[HttpPost]
	public ActionResult<DataImportReturnObject> Post([FromBody] dynamic jsonString) {
		int rowNumber = 1;
		if (CreateUserObject(User) is not UserObject user) {
			return Unauthorized();
		}

		_user = user;
		try {
			string json = jsonString.ToString();
			json = Regex.Replace(
				json,
				Regex.Escape("hierarchy id"),
				"hierarchyid".Replace("$", "$$"),
				RegexOptions.IgnoreCase
			);
			json = Regex.Replace(
				json,
				Regex.Escape("measure id"),
				"measureid".Replace("$", "$$"),
				RegexOptions.IgnoreCase
			);

			var jsonObject = JsonNode.Parse(json);
			var dataImport = (Helper.DataImports)int.Parse(jsonObject!["dataImport"]?.ToString() ?? "0");
			string sheetName = jsonObject["sheet"]?.ToString() ?? string.Empty;
			var array = jsonObject["data"];

			// --------------------------------------------------------
			// Process Target
			// --------------------------------------------------------
			switch (dataImport) {
				case Helper.DataImports.Target:
					var listTarget = new List<SheetDataTarget>();
					foreach (var token in (JsonArray)array!) {
						var value = token.Deserialize<SheetDataTarget>(webDefaults)!;
						value.RowNumber = rowNumber++;
						//value.unitId = _measureDefinitionRepository.Find(md=> md.Id == value.MeasureID).UnitId;
						var mRecord = Dbc.MeasureDefinition.Where(md => md.Id == value.MeasureID);
						if (mRecord.Any()) {
							value.Precision = mRecord.First().Precision;
							listTarget.Add(value);
						}
						else {
							result.Error.Add(new DataImportErrorReturnObject { Row = value.RowNumber, Message = Resource.DI_ERR_TARGET_NO_EXIST });
						}
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
						return result;
					}

					// Find duplicates
					var duplicates = from t in listTarget
									 group t by new { t.HierarchyID, t.MeasureID } into g
									 where g.Count<SheetDataTarget>() > 1
									 select g.Key;
					if (duplicates.Any()) {
						var duplicateRows = from t in listTarget
											join d in duplicates
											on new { a = t.HierarchyID, b = t.MeasureID }
											equals new { a = d.HierarchyID, b = d.MeasureID }
											select t.RowNumber;

						foreach (var itemRow in duplicateRows) {
							result.Error.Add(new() { Row = itemRow, Message = Resource.DI_ERR_TARGET_REPEATED });
						}

						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
						return result;
					}

					foreach (var row in listTarget) {
						ValidateTargetRows(row, _user.Id);
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
					}
					else {
						foreach (var row in listTarget) {
							ImportTargetRecords(row, _user.Id);
						}

						result.Data = null;
						AddAuditTrail(Dbc,
							Resource.WEB_PAGES,
							"WEB-06",
							Resource.DATA_IMPORT,
							@"Target Imported" + " / sheetName=" + sheetName,
							DateTime.Now,
							_user.Id
						);
					}

					return result;
				// --------------------------------------------------------
				// Process Customer Hierarchy
				// --------------------------------------------------------
				case Helper.DataImports.Customer when Config.UsesCustomer:
					var listCustomer = new List<SheetDataCustomer>();
					foreach (var token in (JsonArray)array!) {
						var value = token.Deserialize<SheetDataCustomer>(webDefaults);
						if (value == null) { continue; }
						ValidateCustomerRows(value, _user.Id);
						value!.rowNumber = rowNumber++;
						listCustomer.Add(value);
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
					}
					else {
						foreach (var row in listCustomer) {
							ImportCustomerRecords(row);
						}

						result.Data = null;

						AddAuditTrail(Dbc,
							Resource.WEB_PAGES,
							"WEB-06",
							Resource.DATA_IMPORT,
							@"Customer Imported" + " / sheetName=" + sheetName,
							DateTime.Now,
							_user.Id
						);
					}

					return result;
				// --------------------------------------------------------
				// Process MeasureData
				// --------------------------------------------------------
				case Helper.DataImports.MeasureData:
					var calId = jsonObject["calendarId"];
					var calendarId = int.Parse(calId!.ToString());
					var calendar = Dbc.Calendar.Include(c => c.Interval).Where(c => c.Id == calendarId).First();
					var listMeasureData = new List<SheetDataMeasureData>();

					// From settings page, DO NOT USE = !Active
					if (Dbc.Setting.First().Active == true) {
						if (IsDataLocked(calendar.Interval.Id, _user.Id, calendar, Dbc)) {
							throw new Exception(Resource.DI_ERR_USER_DATE);
						}
					}

					foreach (var token in (JsonArray)array!) {
						var value = token.Deserialize<SheetDataMeasureData>(webDefaults);
						value!.RowNumber = rowNumber++;
						var mdef = Dbc.MeasureDefinition.Where(md => md.Id == value.MeasureID).ToArray();
						if (mdef.Any()) {
							value.UnitId = mdef.First().UnitId;
							value.Precision = mdef.First().Precision;
							listMeasureData.Add(value);
						}
						else {
							result.Error.Add(new() { Row = value.RowNumber, Message = Resource.DI_ERR_NO_MEASURE });
						}
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
						return result;
					}

					foreach (var row in listMeasureData) {
						ValidateMeasureDataRows(row, calendar.Interval.Id, calendar.Id, _user.Id);
					}

					if (result.Error.Count != 0) {
						var temp = result.Error.OrderBy(e => e.Row);
						result.Error = temp.ToList();
					}
					else {
						foreach (var row in listMeasureData) {
							ImportMeasureDataRecords(row, calendarId, _user.Id);
						}

						result.Data = null;

						AddAuditTrail(Dbc,
							Resource.WEB_PAGES,
							"WEB-06",
							Resource.DATA_IMPORT,
							@"Measure Data Imported" +
								" / CalendarId=" + calendar.Id.ToString() +
								" / Interval=" + Dbc.Interval.Where(i => i.Id == calendar.Interval.Id).First().Name +
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
			return new DataImportReturnObject { Error = new() { new() { Id = record.Id, Row = null, Message = e.Message } } };
		}
	}

	private DataImportsMainObject? DataReturn(UserObject user) {
		try {
			var returnObject = new DataImportsMainObject {
				Years = Dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsObject { Year = c.Year, Id = c.Id }).ToArray(),
				CalculationTime = "00:01:00",
				DataImport = new List<DataImportObject>() { DataImportHeading(Helper.DataImports.MeasureData) },
				Intervals = Dbc.Interval.Select(i => new IntervalsObject { Id = i.Id, Name = i.Name }).ToArray(),
				IntervalId = Config.DefaultInterval,
				CalendarId = FindPreviousCalendarId(Dbc.Calendar, Config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = Dbc.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.CalculationTime = CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
				returnObject.DataImport.Add(DataImportHeading(Helper.DataImports.Target));

				if (Config.UsesCustomer) {
					DataImportObject customerRegionData = DataImportHeading(Helper.DataImports.Customer);
					returnObject.DataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			ErrorProcessing(Dbc, e, _user.Id);
			return null;
		}
	}

	private bool IsHierarchyValidated(int rowNumber, int hierarchyId, double? value, int userId) {
		// Commmented out because Michael wanted to import values.
		//this is for a special case where some level 2 hierarchies cannot be edited since they are a sum value
		//if ( value != null )
		//{
		//  if ( ! Helper.canEditValueFromSpecialHierarchy(hierarchyId) )
		//  {
		//    returnObject.error.Add(new DataImportErrorReturnObject { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_VALUE });
		//    return false;
		//  }
		//}
		var hierarchy = Dbc.Hierarchy.Where(h => h.Id == hierarchyId).Select(h => h.Active ?? false).ToArray();
		if (!hierarchy.Any()) {
			result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_EXIST });
			return false;
		}
		else if (!hierarchy.Any(x => x)) {
			result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_ACTIVE });
			return false;
		}
		else if (!Dbc.UserHierarchy.Where(u => u.UserId == userId && u.HierarchyId == hierarchyId).Any()) {
			result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_ACCESS });
			return false;
		}

		return true;
	}

	// --------------------------------------------------------
	// MeasureData
	// --------------------------------------------------------
	private void ValidateMeasureDataRows(SheetDataMeasureData row, int intervalId, int calendarId, int userId) {
		//check for null values
		if (row.MeasureID is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_MEASURE_NULL });
			return;
		}

		if (row.HierarchyID is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL });
			return;
		}

		//check userHierarchy
		IsHierarchyValidated(row.RowNumber, row.HierarchyID ?? -1, row.Value, userId);

		//check measureData
		if (!Dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID
				&& md.CalendarId == calendarId).Any()) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE_DATA });
		}

		//check Measure Definition percentage Unit
		if (row.Value is double v && row.UnitId == 1 && (v < 0 || v > 1)) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.VAL_VALUE_UNIT });
		}

		//check Measure
		var measures = Dbc.Measure.Where(m => m.Active == true && m.HierarchyId == row.HierarchyID
			&& m.MeasureDefinitionId == row.MeasureID)
			.Include(m => m.MeasureDefinition)
			.AsNoTrackingWithIdentityResolution()
			.ToArray();
		if (measures.Length == 0) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE });
		}

		foreach (var m in measures) {
			MeasureCalculatedObject measureCalculated = new() {
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
			if (IsMeasureCalculated(Dbc, bMdExpression, hId, intervalId, row.MeasureID ?? -1, measureCalculated)) {
				result.Error.Add(new() {
					Row = row.RowNumber,
					Message = Resource.DI_ERR_CALCULATED
				});
			}
		}
	}

	private void ImportMeasureDataRecords(SheetDataMeasureData row, int calendarId, int userId) {
		try {
			double? sheetValue = row.Value switch {
				double value => Math.Round(value, row.Precision, MidpointRounding.AwayFromZero),
				_ => null
			};

			_ = Dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID && md.CalendarId == calendarId)
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
	// Target
	// --------------------------------------------------------
	private void ValidateTargetRows(SheetDataTarget row, int userId) {
		//check for null values
		if (row.MeasureID is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_MEASURE_NULL });
			return;
		}

		if (row.HierarchyID is null) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL });
			return;
		}

		//check userHierarchy
		IsHierarchyValidated(row.RowNumber, (int)row.HierarchyID, null, userId);
		var units = Dbc.Target.Where(t => t.Active == true
				&& t.Measure!.MeasureDefinitionId == row.MeasureID
				&& t.Measure.HierarchyId == row.HierarchyID)
			.Select(t => t.Measure!.MeasureDefinition!.UnitId)
			.Distinct()
			.ToArray();
		if (units.Any()) {
			foreach (var unit in units) {
				if (row.Target != null && unit == 1 && (row.Target < 0 || row.Target > 1)) {
					result.Error.Add(new() { Row = row.RowNumber, Message = Resource.VAL_TARGET_UNIT });
				}

				if (row.Yellow != null && unit == 1 && (row.Yellow < 0 || row.Yellow > 1)) {
					result.Error.Add(new() { Row = row.RowNumber, Message = Resource.VAL_YELLOW_UNIT });
				}
			}
		}
		else {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_TARGET_NO_EXIST });
		}
	}

	private void ImportTargetRecords(SheetDataTarget row, int userId) {
		var q1 = Dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
			&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Value != null)
			.Select(t => new { t.MeasureId, t.Value })
			.FirstOrDefault();
		long measureId = q1?.MeasureId ?? -1;
		if (measureId > 0) {
			try {
				if (q1?.Value is null) {
					_ = Dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, (byte)IsProcessed.Complete)
							.SetProperty(t => t.Value, row.Target)
							.SetProperty(t => t.YellowValue, row.Yellow.RoundNullable(row.Precision))
							.SetProperty(t => t.UserId, userId));
				}
				else {
					_ = Dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, (byte)IsProcessed.Complete)
							.SetProperty(t => t.Active, false)
							.SetProperty(t => t.LastUpdatedOn, DateTime.Now));
					_ = Dbc.Target.Add(new() {
						MeasureId = measureId,
						Value = row.Target,
						YellowValue = row.Yellow.RoundNullable(row.Precision),
						Active = true,
						UserId = userId
					});
					_ = Dbc.SaveChanges();
				}
			}
			catch {
				result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_UPLOADING });
			}
		}
	}

	// --------------------------------------------------------
	// Customer Hierarchy
	// --------------------------------------------------------
	private void ValidateCustomerRows(SheetDataCustomer row, int userId) {
		//check for null values
		if (row.HierarchyId is null) {
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL });
			return;
		}

		if (row.CalendarId is null) {
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_CALENDAR_NULL });
			return;
		}

		//check userHierarchy
		_ = IsHierarchyValidated(row.rowNumber, (int)row.HierarchyId, null, userId);

		//check if CalendarId exists
		if (Dbc.Calendar.Find(row.CalendarId) is null) {
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_CALENDAR_NO_EXIST });
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
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_UPLOADING });
		}
	}
}
