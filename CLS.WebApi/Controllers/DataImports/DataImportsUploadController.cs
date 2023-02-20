using CLS.WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CLS.WebApi.Controllers.DataImports;

[ApiController]
[Route("api/dataimports/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
	private readonly JsonSerializerOptions webDefaults = new(JsonSerializerDefaults.Web);
	private readonly ConfigurationObject _config;
	private readonly ApplicationDbContext _context;
	private DataImportReturnObject returnObject = new() { data = new(), error = new() };
	private int calendarId = -1;
	private UserObject _user = new();

	public UploadController(IOptions<ConfigurationObject> config, ApplicationDbContext context) {
		_config = config.Value;
		_context = context;
	}

	public class Model
	{
		[Required]
		public int DataImport { get; set; }

		[Required]
		public string Sheet { get; set; } = null!;

		public IReadOnlyDictionary<string, IEnumerable<SheetDataTarget>> Data { get; set; } = null!;

		public IReadOnlyDictionary<string, IEnumerable<SheetDataTarget>> Targets { get; set; } = null!;

		public IReadOnlyDictionary<string, IEnumerable<SheetDataCustomer>> Customers { get; set; } = null!;

		public IReadOnlyDictionary<string, IEnumerable<SheetDataMeasureData>> MeasureData { get; set; } = null!;

		[Required]
		public int CalendarId { get; set; }
	}

	[HttpPost]
	public ActionResult<DataImportReturnObject> Post([FromBody] dynamic jsonString) {
		int rowNumber = 1;

		try {
			if (Helper.CreateUserObject(User) is UserObject u) {
				_user = u;
			}
			else {
				return Unauthorized();
			}

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
			var array = jsonObject["data"]![sheetName];

			// --------------------------------------------------------
			// Process Target
			// --------------------------------------------------------
			if (dataImport == (int)Helper.dataImports.target) {
				var listTarget = new List<SheetDataTarget>();
				foreach (var token in (JsonArray)array!) {
					var value = token.Deserialize<SheetDataTarget>(webDefaults)!;
					value.RowNumber = rowNumber++;
					//value.unitId = _measureDefinitionRepository.Find(md=> md.Id == value.MeasureID).UnitId;
					var mRecord = _context.MeasureDefinition.Where(md => md.Id == value.MeasureID);
					if (mRecord.Any()) {
						value.Precision = mRecord.First().Precision;
						listTarget.Add(value);
					}
					else {
						returnObject.error.Add(new DataImportErrorReturnObject { row = value.RowNumber, message = Resource.DI_ERR_TARGET_NO_EXIST });
					}
				}

				if (returnObject.error.Count != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
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
						returnObject.error.Add(new() { row = itemRow, message = Resource.DI_ERR_TARGET_REPEATED });
					}

					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
				}

				var task = Task.Factory.StartNew(() => Parallel.ForEach(
					listTarget,
					/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
					item => ValidateTargetRows(item, _user.userId)));
				TaskList.Add(task);
				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listTarget,
															/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
															item => ImportTargetRecords(item, _user.userId)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.AddAuditTrail(_context,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Target Imported" + " / sheetName=" + sheetName,
						DateTime.Now,
						_user.userId
					);

					return returnObject;
				}
			}
			// --------------------------------------------------------
			// Process Customer Hierarchy
			// --------------------------------------------------------
			else if (dataImport == (int)Helper.dataImports.customer && _config.usesCustomer) {
				var listCustomer = new List<SheetDataCustomer>();
				foreach (var token in (JsonArray)array!) {
					var value = token.Deserialize<SheetDataCustomer>(webDefaults);
					value!.rowNumber = rowNumber++;
					listCustomer.Add(value);
				}

				var task = Task.Factory.StartNew(() => Parallel.ForEach(
					listCustomer,
					/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
					item => ValidateCustomerRows(item, _user.userId)));
				TaskList.Add(task);
				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listCustomer,
															//new ParallelOptions {MaxDegreeOfParallelism = 2},
															item => ImportCustomerRecords(item)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.AddAuditTrail(_context,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Customer Imported" + " / sheetName=" + sheetName,
						DateTime.Now,
						_user.userId
					);

					return returnObject;
				}
			}
			// --------------------------------------------------------
			// Process MeasureData
			// --------------------------------------------------------
			else {
				var calId = jsonObject["calendarId"];
				calendarId = int.Parse(calId!.ToString());
				var calendar = _context.Calendar.Include(c => c.Interval).Where(c => c.Id == calendarId).First();
				var listMeasureData = new List<SheetDataMeasureData>();

				// From settings page, DO NOT USE = !Active
				if (_context.Setting.First().Active == true) {
					if (Helper.IsDataLocked(calendar.Interval.Id, _user.userId, calendar, _context)) {
						throw new Exception(Resource.DI_ERR_USER_DATE);
					}
				}

				foreach (var token in (JsonArray)array!) {
					var value = token.Deserialize<SheetDataMeasureData>(webDefaults);
					value!.rowNumber = rowNumber++;

					var mRecord = _context.MeasureDefinition.Where(md => md.Id == value.MeasureID);

					if (mRecord.Any()) {
						value.unitId = mRecord.First().UnitId;
						value.precision = mRecord.First().Precision;
						listMeasureData.Add(value);
					}
					else {
						returnObject.error.Add(new() { row = value.rowNumber, message = Resource.DI_ERR_NO_MEASURE });
					}
				}

				if (returnObject.error.Count != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
				}


				var task = Task.Factory.StartNew(() => Parallel.ForEach(
													   listMeasureData,
													   /*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
													   item => ValidateMeasureDataRows(item, calendar.Interval.Id, calendar.Id, _user.userId)));
				TaskList.Add(task);

				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return returnObject;
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listMeasureData,
															/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
															item => ImportMeasureDataRecords(item, _user.userId)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.AddAuditTrail(_context,
						Resource.WEB_PAGES,
						"WEB-06",
						Resource.DATA_IMPORT,
						@"Measure Data Imported" +
							" / CalendarId=" + calendar.Id.ToString() +
							" / Interval=" + _context.Interval.Where(i => i.Id == calendar.Interval.Id).First().Name +
							" / Year=" + calendar.Year.ToString() +
							" / Quarter=" + calendar.Quarter.ToString() +
							" / Month=" + calendar.Month.ToString() +
							" / Week=" + calendar.WeekNumber.ToString() +
							" / sheetName=" + sheetName,
						DateTime.Now,
						_user.userId
					);

					return returnObject;
				}
			}
		}
		catch (Exception e) {
			returnObject = new() { error = new() };
			var newReturn = Helper.ErrorProcessingDataImport(_context, e, _user!.userId);
			returnObject.error.Add(new() { id = newReturn.error.Id, row = null, message = newReturn.error.Message });
			return returnObject;
		}
	}

	private DataImportsMainObject? DataReturn(UserObject user) {
		try {
			var returnObject = new DataImportsMainObject {
				years = new(),
				//calculationTime = new CalculationTimeObject(), 
				calculationTime = "00:01:00",
				dataImport = new(),
				intervals = new(),
				intervalId = _config.DefaultInterval,
				calendarId = Helper.FindPreviousCalendarId(_context.Calendar, _config.DefaultInterval)
			};

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _context.Setting.First().CalculateSchedule ?? string.Empty;
			returnObject.calculationTime = Helper.CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			//years
			var years = _context.Calendar
						.Where(c => c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var year in years) {
				returnObject.years.Add(new() { year = year.Year, id = year.Id });
			}

			//intervals
			var intervals = _context.Interval;
			foreach (var interval in intervals) {
				returnObject.intervals.Add(new IntervalsObject {
					id = interval.Id,
					name = interval.Name
				});
			}

			//dataImport
			DataImportObject measureData = Helper.DataImportHeading(Helper.dataImports.measureData);
			returnObject.dataImport.Add(measureData);

			if (user.userRoleId == (int)Helper.userRoles.systemAdministrator) {
				DataImportObject targetData = Helper.DataImportHeading(Helper.dataImports.target);
				returnObject.dataImport.Add(targetData);

				if (_config.usesCustomer) {
					DataImportObject customerRegionData = Helper.DataImportHeading(Helper.dataImports.customer);
					returnObject.dataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			Helper.ErrorProcessing(_context, e, _user.userId);
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
		var hierarchy = _context.Hierarchy.Where(h => h.Id == hierarchyId).Select(h => h.Active ?? false).ToArray();
		if (!hierarchy.Any()) {
			returnObject.error.Add(new() { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_EXIST });
			return false;
		}
		else if (!hierarchy.Any(x => x)) {
			returnObject.error.Add(new() { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_ACTIVE });
			return false;
		}
		else if (!_context.UserHierarchy.Where(u => u.Id == userId && u.HierarchyId == hierarchyId).Any()) {
			returnObject.error.Add(new() { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_ACCESS });
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
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_MEASURE_NULL });
			return;
		}

		if (row.HierarchyID is null) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });
			return;
		}

		//check userHierarchy
		IsHierarchyValidated(row.rowNumber, row.HierarchyID ?? -1, row.Value, userId);

		//check measureData
		if (!_context.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID
				&& md.CalendarId == calendarId).Any()) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_NO_MEASURE_DATA });
		}

		//check Measure Definition Unit
		if (row.Value is double v && row.unitId == 1 && (v < 0 || v > 1)) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.VAL_VALUE_UNIT });
		}

		//check Measure
		var mco = _context.Measure.Where(m => m.Active == true && m.HierarchyId == row.HierarchyID
			&& m.MeasureDefinitionId == row.MeasureID)
			.Include(m => m.MeasureDefinition)
			.ToArray();
		if (mco.Any()) {
			foreach (var r in mco) {
				var measureCalculated = new MeasureCalculatedObject {
					reportIntervalId = r.MeasureDefinition!.ReportIntervalId,
					calculated = r.MeasureDefinition.Calculated ?? false,
					aggDaily = r.MeasureDefinition.AggDaily ?? false,
					aggWeekly = r.MeasureDefinition.AggWeekly ?? false,
					aggMonthly = r.MeasureDefinition.AggMonthly ?? false,
					aggQuarterly = r.MeasureDefinition.AggQuarterly ?? false,
					aggYearly = r.MeasureDefinition.AggYearly ?? false
				};
				var bMdExpression = r.Expression ?? false;
				var hId = r.HierarchyId;
				if (Helper.IsMeasureCalculated(_context, bMdExpression, hId, intervalId, row.MeasureID ?? -1, measureCalculated)) {
					returnObject.error.Add(new() {
						row = row.rowNumber,
						message = Resource.DI_ERR_CALCULATED
					});
				}
			}
		}
		else {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_NO_MEASURE });
		}
	}

	private void ImportMeasureDataRecords(SheetDataMeasureData row, int userId) {
		try {
			double? sheetValue = row.Value switch {
				double value => Math.Round(value, row.precision, MidpointRounding.AwayFromZero),
				_ => null
			};

			_ = _context.MeasureData.Where(md => md.Measure!.HierarchyId == row.HierarchyID
				&& md.Measure.MeasureDefinitionId == row.MeasureID && md.CalendarId == calendarId)
				.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, 1)
					.SetProperty(md => md.UserId, userId)
					.SetProperty(md => md.LastUpdatedOn, DateTime.Now)
					.SetProperty(md => md.Value, md => sheetValue ?? md.Value)
					.SetProperty(md => md.Explanation, md => row.Explanation ?? md.Explanation)
					.SetProperty(md => md.Action, md => row.Action ?? md.Action));
		}
		catch {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_UPLOADING });
		}
	}

	// --------------------------------------------------------
	// Target
	// --------------------------------------------------------
	private void ValidateTargetRows(SheetDataTarget row, int userId) {
		//check for null values
		if (row.MeasureID is null) {
			returnObject.error.Add(new() { row = row.RowNumber, message = Resource.DI_ERR_MEASURE_NULL });
			return;
		}

		if (row.HierarchyID is null) {
			returnObject.error.Add(new() { row = row.RowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });
			return;
		}

		//check userHierarchy
		IsHierarchyValidated(row.RowNumber, (int)row.HierarchyID, null, userId);
		var units = _context.Target.Where(t => t.Active == true
				&& t.Measure!.MeasureDefinitionId == row.MeasureID
				&& t.Measure.HierarchyId == row.HierarchyID)
			.Select(t => t.Measure!.MeasureDefinition!.UnitId)
			.Distinct()
			.ToArray();
		if (units.Any()) {
			foreach (var unit in units) {
				if (row.Target != null && unit == 1 && (row.Target < 0 || row.Target > 1)) {
					returnObject.error.Add(new() { row = row.RowNumber, message = Resource.VAL_TARGET_UNIT });
				}

				if (row.Yellow != null && unit == 1 && (row.Yellow < 0 || row.Yellow > 1)) {
					returnObject.error.Add(new() { row = row.RowNumber, message = Resource.VAL_YELLOW_UNIT });
				}
			}
		}
		else {
			returnObject.error.Add(new() { row = row.RowNumber, message = Resource.DI_ERR_TARGET_NO_EXIST });
		}
	}

	private void ImportTargetRecords(SheetDataTarget row, int userId) {
		var q1 = _context.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
			&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Value != null)
			.Select(t => new { t.MeasureId, t.Value })
			.FirstOrDefault();
		long measureId = q1?.MeasureId ?? -1;
		if (measureId > 0) {
			try {
				if (q1?.Value is null) {
					_ = _context.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, 2)
							.SetProperty(t => t.Value, row.Target)
							.SetProperty(t => t.YellowValue, row.Yellow.RoundNullable(row.Precision))
							.SetProperty(t => t.UserId, userId));
				}
				else {
					_ = _context.Target.Where(t => t.Measure!.HierarchyId == row.HierarchyID
						&& t.Measure.MeasureDefinitionId == row.MeasureID && t.Active == true)
						.ExecuteUpdate(s => s.SetProperty(t => t.IsProcessed, 2)
							.SetProperty(t => t.Active, false)
							.SetProperty(t => t.LastUpdatedOn, DateTime.Now));
					_ = _context.Target.Add(new() {
						MeasureId = measureId,
						Value = row.Target,
						YellowValue = row.Yellow.RoundNullable(row.Precision),
						Active = true,
						UserId = userId
					});
					_ = _context.SaveChanges();
				}
			}
			catch {
				returnObject.error.Add(new() { row = row.RowNumber, message = Resource.DI_ERR_UPLOADING });
			}
		}
	}

	// --------------------------------------------------------
	// Customer Hierarchy
	// --------------------------------------------------------
	private void ValidateCustomerRows(SheetDataCustomer row, int userId) {
		//check for null values
		if (row.HierarchyId is null) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });
			return;
		}

		if (row.CalendarId is null) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_CALENDAR_NULL });
			return;
		}

		//check userHierarchy
		_ = IsHierarchyValidated(row.rowNumber, (int)row.HierarchyId, null, userId);

		//check if CalendarId exists
		if (_context.Calendar.Find(row.CalendarId) is null) {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_CALENDAR_NO_EXIST });
		}
	}

	private void ImportCustomerRecords(SheetDataCustomer row) {
		try {
			_ = _context.CustomerRegion.Add(new() {
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
				IsProcessed = (byte)Helper.IsProcessed.complete,
				HeaderStatusCode = row.HeaderStatusCode,
				HeaderStatus = row.HeaderStatus,
				BlockCode = row.BlockCode,
				BlockText = row.BlockText,
				RejectionCode = row.RejectionCode,
				RejectionText = row.RejectionText,
				CreditStatusCheck = row.CreditStatusCheck,
				CreditCode = row.CreditCode
			});
			_ = _context.SaveChanges();
		}
		catch {
			returnObject.error.Add(new() { row = row.rowNumber, message = Resource.DI_ERR_UPLOADING });
		}
	}
}
