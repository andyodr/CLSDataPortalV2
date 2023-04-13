using CLS.WebApi.Data;
using CLS.WebApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static CLS.WebApi.Helper;

namespace CLS.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
	private readonly JsonSerializerOptions webDefaults = new(JsonSerializerDefaults.Web);
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _dbc;
	private DataImportReturnObject result = new() { Data = new(), Error = new() };
	private int calendarId = -1;
	private UserObject _user = new();

	public UploadController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_dbc = context;
	}

	public class Model
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
			List<Task> TaskList = new();
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
			int dataImport = int.Parse(jsonObject!["dataImport"]?.ToString() ?? "0");

			string sheetName = jsonObject["sheet"]?.ToString() ?? string.Empty;
			var array = jsonObject["data"];

			// --------------------------------------------------------
			// Process Target
			// --------------------------------------------------------
			if (dataImport == (int)dataImports.target) {
				var listTarget = new List<SheetDataTarget>();
				foreach (var token in (JsonArray)array!) {
					var value = token.Deserialize<SheetDataTarget>(webDefaults)!;
					value.RowNumber = rowNumber++;
					//value.unitId = _measureDefinitionRepository.Find(md=> md.Id == value.MeasureID).UnitId;
					var mRecord = _dbc.MeasureDefinition.Where(md => md.Id == value.MeasureID);
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
								 where g.Count() > 1
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
					return result;
				}
				else {
					TaskList.Clear();
					foreach (var row in listTarget) {
						ImportTargetRecords(row, _user.Id);
					}

					result.Data = null;
					AddAuditTrail(_dbc,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Target Imported" + " / sheetName=" + sheetName,
						DateTime.Now,
						_user.Id
					);

					return result;
				}
			}
			// --------------------------------------------------------
			// Process Customer Hierarchy
			// --------------------------------------------------------
			else if (dataImport == (int)dataImports.customer && _config.usesCustomer) {
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
					return result;
				}
				else {
					TaskList.Clear();
					foreach (var row in listCustomer) {
						ImportCustomerRecords(row);
					}

					result.Data = null;

					AddAuditTrail(_dbc,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Customer Imported" + " / sheetName=" + sheetName,
						DateTime.Now,
						_user.Id
					);

					return result;
				}
			}
			// --------------------------------------------------------
			// Process MeasureData
			// --------------------------------------------------------
			else {
				var calId = jsonObject["calendarId"];
				calendarId = int.Parse(calId!.ToString());
				var calendar = _dbc.Calendar.Include(c => c.Interval).Where(c => c.Id == calendarId).First();
				var listMeasureData = new List<SheetDataMeasureData>();

				// From settings page, DO NOT USE = !Active
				if (_dbc.Setting.First().Active == true) {
					if (IsDataLocked(calendar.Interval.Id, _user.Id, calendar, _dbc)) {
						throw new Exception(Resource.DI_ERR_USER_DATE);
					}
				}

				foreach (var token in (JsonArray)array!) {
					var value = token.Deserialize<SheetDataMeasureData>(webDefaults);
					value!.RowNumber = rowNumber++;

					var mRecord = _dbc.MeasureDefinition.Where(md => md.Id == value.MeasureID);

					if (mRecord.Any()) {
						value.UnitId = mRecord.First().UnitId;
						value.Precision = mRecord.First().Precision;
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
					return result;
				}
				else {
					TaskList.Clear();
					foreach (var row in listMeasureData) {
						ImportMeasureDataRecords(row, _user.Id);
					}

					result.Data = null;

					AddAuditTrail(_dbc,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Measure Data Imported" +
							" / CalendarId=" + calendar.Id.ToString() +
							" / Interval=" + _dbc.Interval.Where(i => i.Id == calendar.Interval.Id).First().Name +
							" / Year=" + calendar.Year.ToString() +
							" / Quarter=" + calendar.Quarter.ToString() +
							" / Month=" + calendar.Month.ToString() +
							" / Week=" + calendar.WeekNumber.ToString() +
							" / sheetName=" + sheetName,
						DateTime.Now,
						_user.Id
					);

					return result;
				}
			}
		}
		catch (Exception e) {
			var record = _dbc.AuditTrail.Add(new AuditTrail {
				UpdatedBy = _user!.Id,
				Type = Resource.WEB_PAGES,
				Code = "WEB-06",
				Data = e.Message + "\n" + (e.StackTrace?.ToString() ?? string.Empty),
				Description = Resource.DATA_IMPORT,
				LastUpdatedOn = DateTime.Now
			}).Entity;
			_dbc.SaveChanges();
			return new DataImportReturnObject { Error = new() { new() { Id = record.Id, Row = null, Message = e.Message } } };
		}
	}

	private DataImportsMainObject? DataReturn(UserObject user) {
		try {
			var returnObject = new DataImportsMainObject {
				Years = _dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Yearly)
						.OrderByDescending(y => y.Year).Select(c => new YearsObject { year = c.Year, id = c.Id }).ToArray(),
				CalculationTime = "00:01:00",
				DataImport = new List<DataImportObject>() { DataImportHeading(dataImports.measureData) },
				Intervals = _dbc.Interval.Select(i => new IntervalsObject { Id = i.Id, Name = i.Name }).ToArray(),
				IntervalId = _config.DefaultInterval,
				CalendarId = FindPreviousCalendarId(_dbc.Calendar, _config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _dbc.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.CalculationTime = CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			if (User.IsInRole(Roles.SystemAdministrator.ToString())) {
				returnObject.DataImport.Add(DataImportHeading(dataImports.target));

				if (_config.usesCustomer) {
					DataImportObject customerRegionData = DataImportHeading(dataImports.customer);
					returnObject.DataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			ErrorProcessing(_dbc, e, _user.Id);
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
		var hierarchy = _dbc.Hierarchy.Where(h => h.Id == hierarchyId).Select(h => h.Active ?? false).ToArray();
		if (!hierarchy.Any()) {
			result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_EXIST });
			return false;
		}
		else if (!hierarchy.Any(x => x)) {
			result.Error.Add(new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_ACTIVE });
			return false;
		}
		else if (!_dbc.UserHierarchy.Where(u => u.UserId == userId && u.HierarchyId == hierarchyId).Any()) {
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
		if (!_dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID
				&& md.CalendarId == calendarId).Any()) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE_DATA });
		}

		//check Measure Definition percentage Unit
		if (row.Value is double v && row.UnitId == 1 && (v < 0 || v > 1)) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.VAL_VALUE_UNIT });
		}

		//check Measure
		var measures = _dbc.Measure.Where(m => m.Active == true && m.HierarchyId == row.HierarchyID
			&& m.MeasureDefinitionId == row.MeasureID)
			.Include(m => m.MeasureDefinition)
			.AsNoTrackingWithIdentityResolution()
			.ToArray();
		if (measures.Length == 0) {
			result.Error.Add(new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE });
		}

		foreach (var m in measures) {
			MeasureCalculatedObject measureCalculated = new() {
				reportIntervalId = m.MeasureDefinition!.ReportIntervalId,
				calculated = m.MeasureDefinition.Calculated ?? false,
				aggDaily = m.MeasureDefinition.AggDaily ?? false,
				aggWeekly = m.MeasureDefinition.AggWeekly ?? false,
				aggMonthly = m.MeasureDefinition.AggMonthly ?? false,
				aggQuarterly = m.MeasureDefinition.AggQuarterly ?? false,
				aggYearly = m.MeasureDefinition.AggYearly ?? false
			};
			var bMdExpression = m.Expression ?? false;
			var hId = m.HierarchyId;
			if (IsMeasureCalculated(_dbc, bMdExpression, hId, intervalId, row.MeasureID ?? -1, measureCalculated)) {
				result.Error.Add(new() {
					Row = row.RowNumber,
					Message = Resource.DI_ERR_CALCULATED
				});
			}
		}
	}

	private void ImportMeasureDataRecords(SheetDataMeasureData row, int userId) {
		try {
			double? sheetValue = row.Value switch {
				double value => Math.Round(value, row.Precision, MidpointRounding.AwayFromZero),
				_ => null
			};

			_ = _dbc.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID && md.CalendarId == calendarId)
				.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, 1)
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
		var units = _dbc.Target.Where(t => t.Active == true
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
		var q1 = _dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
			&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Value != null)
			.Select(t => new { t.MeasureId, t.Value })
			.FirstOrDefault();
		long measureId = q1?.MeasureId ?? -1;
		if (measureId > 0) {
			try {
				if (q1?.Value is null) {
					_ = _dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, 2)
							.SetProperty(t => t.Value, row.Target)
							.SetProperty(t => t.YellowValue, row.Yellow.RoundNullable(row.Precision))
							.SetProperty(t => t.UserId, userId));
				}
				else {
					_ = _dbc.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, 2)
							.SetProperty(t => t.Active, false)
							.SetProperty(t => t.LastUpdatedOn, DateTime.Now));
					_ = _dbc.Target.Add(new() {
						MeasureId = measureId,
						Value = row.Target,
						YellowValue = row.Yellow.RoundNullable(row.Precision),
						Active = true,
						UserId = userId
					});
					_ = _dbc.SaveChanges();
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
		if (_dbc.Calendar.Find(row.CalendarId) is null) {
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_CALENDAR_NO_EXIST });
		}
	}

	private void ImportCustomerRecords(SheetDataCustomer row) {
		try {
			_ = _dbc.CustomerRegion.Add(new() {
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
				IsProcessed = (byte)IsProcessed.complete,
				HeaderStatusCode = row.HeaderStatusCode,
				HeaderStatus = row.HeaderStatus,
				BlockCode = row.BlockCode,
				BlockText = row.BlockText,
				RejectionCode = row.RejectionCode,
				RejectionText = row.RejectionText,
				CreditStatusCheck = row.CreditStatusCheck,
				CreditCode = row.CreditCode
			});
			_ = _dbc.SaveChanges();
		}
		catch {
			result.Error.Add(new() { Row = row.rowNumber, Message = Resource.DI_ERR_UPLOADING });
		}
	}
}
