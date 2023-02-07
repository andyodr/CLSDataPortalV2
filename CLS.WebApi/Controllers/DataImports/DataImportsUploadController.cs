using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CLS.WebApi.Controllers.DataImports;

[Authorize]
[Route("api/dataimports/[controller]")]
public class UploadController : ControllerBase
{
	private IMeasureRepository _measureRepository;
	private IMeasureDataRepository _measureDataRepository;
	private IMeasureDefinitionRepository _measureDefinitionRepository;
	private IAuditTrailRepository _auditTrailRepository;
	private IUserHierarchyRepository _userHierarchyRepository;
	private IUserRepository _userRepository;
	private IIntervalRepository _intervalRepository;
	private ICalendarRepository _calendarRepository;
	private IUserCalendarLockRepository _userCalendarLockRepository;
	private ISettingRepository _settingRepository;
	private List<SheetDataMeasureData> listMeasureData = new List<SheetDataMeasureData>();
	private List<SheetDataTarget> listTarget = new List<SheetDataTarget>();
	private List<SheetDataCustomer> listCustomer = new List<SheetDataCustomer>();
	private DataImportReturnObject returnObject = new DataImportReturnObject { data = new DataImportsMainObject(), error = new List<DataImportErrorReturnObject>() };
	private int calendarId = -1;
	private UserObject _user = new UserObject();

	public UploadController(IUserCalendarLockRepository userCalendarLockRepository,
							IMeasureDefinitionRepository measureDefinitionRepository,
							IIntervalRepository intervalRepository,
							ICalendarRepository calendarRepository,
							IUserRepository userRepository,
							IUserHierarchyRepository userHierarchyRepository,
							IMeasureDataRepository measureDataRepository,
							IMeasureRepository measureRepository,
							ISettingRepository settingRepository,
							IAuditTrailRepository auditTrailRepository) {
		_userCalendarLockRepository = userCalendarLockRepository;
		_intervalRepository = intervalRepository;
		_calendarRepository = calendarRepository;
		_measureDataRepository = measureDataRepository;
		_measureRepository = measureRepository;
		_auditTrailRepository = auditTrailRepository;
		_userRepository = userRepository;
		_userHierarchyRepository = userHierarchyRepository;
		_measureDefinitionRepository = measureDefinitionRepository;
		_settingRepository = settingRepository;
	}

	// POST api/values
	[HttpPost]
	public string Post([FromBody] dynamic jsonString) {
		int rowNumber = 1;

		try {
			_user = Helper.UserAuthorization(User);

			if (_user == null)
				throw new Exception();
			if (!Helper.IsUserPageAuthorized(Helper.pages.dataImports, _user.userRoleId))
				throw new Exception(Resource.PAGE_AUTHORIZATION_ERR);

			List<Task> TaskList = new List<Task>();
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

			JObject jsonObject = JObject.Parse(json);

			int dataImport = int.Parse(jsonObject["dataImport"].ToString());

			string sheetName = jsonObject["sheet"].ToString();
			//var columnHeaders = jsonObject["columns"].ToList();
			var sheet = jsonObject["data"].ToObject<JObject>();
			var data = sheet[sheetName].ToList();

			// --------------------------------------------------------
			// Process Target
			// --------------------------------------------------------
			if (dataImport == (int)Helper.dataImports.target) {

				foreach (var token in data) {
					SheetDataTarget value = JsonConvert.DeserializeObject<SheetDataTarget>(token.ToString());
					value.rowNumber = rowNumber++;
					//value.unitId = _measureDefinitionRepository.Find(md=> md.Id == value.MeasureID).UnitId;
					var mRecord = _measureDefinitionRepository.All().Where(md => md.Id == value.MeasureID);
					if (mRecord.Count() > 0) {
						value.precision = mRecord.First().Precision;
						listTarget.Add(value);
					}
					else {
						returnObject.error.Add(new DataImportErrorReturnObject { row = value.rowNumber, message = Resource.DI_ERR_TARGET_NO_EXIST });
					}
				}

				if (returnObject.error.Count() != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}

				// Find duplicates
				var duplicates = from t in listTarget
								 group t by new { t.HierarchyID, t.MeasureID } into g
								 where g.Count() > 1
								 select g.Key;
				if (duplicates.Count() > 0) {

					var duplicateRows = from t in listTarget
										join d in duplicates
										on new { a = t.HierarchyID, b = t.MeasureID }
										equals new { a = d.HierarchyID, b = d.MeasureID }
										select t.rowNumber;

					foreach (var itemRow in duplicateRows) {
						returnObject.error.Add(new DataImportErrorReturnObject { row = itemRow, message = Resource.DI_ERR_TARGET_REPEATED });
					}
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}

				var task = Task.Factory.StartNew(() => Parallel.ForEach(
				  listTarget,
				  /*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
				  item => validateTargetRows(item, _user.userId)));
				TaskList.Add(task);

				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count() != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listTarget,
															/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
															item => importTargetRecords(item, _user.userId)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.addAuditTrail(
					  Resource.WEB_PAGES,
					   "WEB-06",
					   Resource.DATA_IMPORT,
					   @"Target Imported" + " / sheetName=" + sheetName,
					   DateTime.Now,
					   _user.userId
					);

					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}

			}
			// --------------------------------------------------------
			// Process Customer Hierarchy
			// --------------------------------------------------------
			else if (dataImport == (int)Helper.dataImports.customer && Startup.ConfigurationJson.usesCustomer) {
				foreach (var token in data) {
					SheetDataCustomer value = JsonConvert.DeserializeObject<SheetDataCustomer>(token.ToString());
					value.rowNumber = rowNumber++;
					listCustomer.Add(value);
				}

				var task = Task.Factory.StartNew(() => Parallel.ForEach(
				  listCustomer,
				  /*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
				  item => validateCustomerRows(item, _user.userId)));
				TaskList.Add(task);

				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count() != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listCustomer,
															//new ParallelOptions {MaxDegreeOfParallelism = 2},
															item => importCustomerRecords(item)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.addAuditTrail(
					  Resource.WEB_PAGES,
					   "WEB-06",
					   Resource.DATA_IMPORT,
					   @"Customer Imported" + " / sheetName=" + sheetName,
					   DateTime.Now,
					   _user.userId
					);

					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}
			}
			// --------------------------------------------------------
			// Process MeasureData
			// --------------------------------------------------------
			else {

				var calId = jsonObject["calendarId"];
				calendarId = int.Parse(calId.ToString());
				var calendar = _calendarRepository.All().Where(c => c.Id == calendarId).First();

				// From settings page, DO NOT USE = !Active
				if (_settingRepository.All().First().Active == true) {
					if (Helper.isDataLocked(calendar.IntervalId, _user.userId, calendar, _calendarRepository, _userCalendarLockRepository))
						throw new Exception(Resource.DI_ERR_USER_DATE);
				}

				foreach (var token in data) {
					SheetDataMeasureData value = JsonConvert.DeserializeObject<SheetDataMeasureData>(token.ToString());
					value.rowNumber = rowNumber++;

					var mRecord = _measureDefinitionRepository.All().Where(md => md.Id == value.MeasureID);

					if (mRecord.Count() > 0) {
						value.unitId = mRecord.First().UnitId;
						value.precision = mRecord.First().Precision;
						listMeasureData.Add(value);
					}
					else {
						returnObject.error.Add(new DataImportErrorReturnObject { row = value.rowNumber, message = Resource.DI_ERR_NO_MEASURE });
					}
				}

				if (returnObject.error.Count() != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}


				var task = Task.Factory.StartNew(() => Parallel.ForEach(
													   listMeasureData,
													   /*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
													   item => validateMeasureDataRows(item, calendar.IntervalId, calendar.Id, _user.userId)));
				TaskList.Add(task);

				Task.WaitAll(TaskList.ToArray());
				if (returnObject.error.Count() != 0) {
					var temp = returnObject.error.OrderBy(e => e.row);
					returnObject.error = temp.ToList();
					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}
				else {
					TaskList.Clear();
					var task2 = Task.Factory.StartNew(() => Parallel.ForEach(
															listMeasureData,
															/*new ParallelOptions {MaxDegreeOfParallelism = 1},*/
															item => importMeasureDataRecords(item, _user.userId)));
					TaskList.Add(task2);
					Task.WaitAll(TaskList.ToArray());
					//returnObject.data = dataReturn(_user);
					returnObject.data = null;

					Helper.addAuditTrail(
					  Resource.WEB_PAGES,
					   "WEB-06",
					   Resource.DATA_IMPORT,
					   @"Measure Data Imported" +
						  " / CalendarId=" + calendar.Id.ToString() +
						  " / Interval=" + _intervalRepository.All().Where(i => i.Id == calendar.IntervalId).FirstOrDefault().Name +
						  " / Year=" + calendar.Year.ToString() +
						  " / Quarter=" + calendar.Quarter.ToString() +
						  " / Month=" + calendar.Month.ToString() +
						  " / Week=" + calendar.WeekNumber.ToString() +
						  " / sheetName=" + sheetName,
					   DateTime.Now,
					   _user.userId
					);

					return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
				}

			}

		}
		catch (Exception e) {
			returnObject = new DataImportReturnObject { error = new List<DataImportErrorReturnObject>() };
			var newReturn = Helper.ErrorProcessingDataImport(_context, e, _user.userId);
			returnObject.error.Add(new DataImportErrorReturnObject { id = newReturn.error.id, row = null, message = newReturn.error.message });
			return Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);
		}
	}

	private DataImportsMainObject dataReturn(UserObject user) {
		try {
			DataImportsMainObject returnObject = new DataImportsMainObject {
				years = new List<ViewModel.FilterObjects.YearsObject>(),
				//calculationTime = new CalculationTimeObject(), 
				calculationTime = "00:01:00",
				dataImport = new List<DataImportObject>(),
				intervals = new List<intervalsObject>()
			};

			returnObject.intervalId = Helper.defaultIntervalId;
			returnObject.calendarId = Helper.findPreviousCalendarId(_calendarRepository, Helper.defaultIntervalId);

			//returnObject.calculationTime.current = DateTime.Now;
			string sCalculationTime = _settingRepository.All().FirstOrDefault().CalculateSchedule;
			returnObject.calculationTime = Helper.CalculateScheduleStr(sCalculationTime, "HH", ":") + " Hours, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "MM", ":") + " Minutes, " +
										   Helper.CalculateScheduleStr(sCalculationTime, "SS", ":") + " Seconds";

			//years
			var years = _calendarRepository.All()
						.Where(c => c.Interval.Id == (int)Helper.intervals.yearly)
						.OrderByDescending(y => y.Year);

			foreach (var year in years) {
				returnObject.years.Add(new ViewModel.FilterObjects.YearsObject { year = year.Year, id = year.Id });
			}

			//intervals
			var intervals = _intervalRepository.All();
			foreach (var interval in intervals) {
				returnObject.intervals.Add(new intervalsObject {
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

				if (Startup.ConfigurationJson.usesCustomer) {
					DataImportObject customerRegionData = Helper.DataImportHeading(Helper.dataImports.customer);
					returnObject.dataImport.Add(customerRegionData);
				}

			}

			return returnObject;
		}
		catch (Exception e) {
			Helper.ErrorProcessing(_context, e, _auditTrailRepository, HttpContext, user);
			return null;
		}
	}

	private bool isHierarchyValidated(int rowNumber, int hierarchyId, double? value, int userId) {

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

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string hierarchySQL = "SELECT Active FROM Hierarchy WHERE Id = " + hierarchyId;
			string userHierarchySQL = "SELECT Id FROM UserHierarchy WHERE UserId = " + userId + " AND HierarchyId = " + hierarchyId;

			con.Open();

			//check userHierarchy
			SqlCommand cmd = new SqlCommand(hierarchySQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (!rdr.HasRows) {
					returnObject.error.Add(new DataImportErrorReturnObject { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_EXIST });
					return false;
				}
				else {
					while (rdr.Read()) {
						if (!rdr.GetBoolean(0)) {
							returnObject.error.Add(new DataImportErrorReturnObject { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_ACTIVE });
							return false;
						}
					}
				}
			}

			//check userHierarchy
			cmd = new SqlCommand(userHierarchySQL, con);
			result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (!rdr.HasRows) {
					returnObject.error.Add(new DataImportErrorReturnObject { row = rowNumber, message = Resource.DI_ERR_HIEARCHY_NO_ACCESS });
					return false;
				}
			}

		}
		return true;
	}

	// --------------------------------------------------------
	// MeasureData
	// --------------------------------------------------------
	private Action validateMeasureDataRows(SheetDataMeasureData row, int intervalId, int calendarId, int userId) {

		//check for null values
		if (row.MeasureID == null || row.HierarchyID == null) {
			if (row.MeasureID == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_MEASURE_NULL });
			if (row.HierarchyID == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });

			return null;
		}


		//check userHierarchy
		isHierarchyValidated(row.rowNumber, (int)row.HierarchyID, row.Value, userId);

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {

			con.Open();

			//check measureData
			string measureDataSQL = "Select md.Id from MeasureData md " +
			  " inner join Measure m on (m.Id = md.MeasureId) where m.HierarchyId = " + row.HierarchyID +
			  " and m.MeasureDefinitionId = " + row.MeasureID +
			  " and md.CalendarId = " + calendarId;

			SqlCommand cmd = new SqlCommand(measureDataSQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (!rdr.HasRows) {
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_NO_MEASURE_DATA });
				}
			}

			//check Measure Definition Unit
			if (row.Value != null) {
				if (row.unitId == 1 && (row.Value < 0 || row.Value > 1))
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.VAL_VALUE_UNIT });
			}

			//check Measure
			string measureSQL = "SELECT m.Expression, md.ReportIntervalId, md.Calculated, " +
			  " md.AggDaily, md.AggWeekly, md.AggMonthly, md.AggQuarterly, md.AggYearly, m.HierarchyId " +
			  " FROM measure m INNER JOIN MeasureDefinition md ON (md.Id = m.MeasureDefinitionId)" +
			  " WHERE m.Active = 1 " +
			  " AND m.HierarchyId = " + row.HierarchyID + " AND m.MeasureDefinitionId = " + row.MeasureID;

			cmd = new SqlCommand(measureSQL, con);
			result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (!rdr.HasRows) {
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_NO_MEASURE });
				}
				else {
					while (rdr.Read()) {
						if (row.Value != null) {
							MeasureCalculatedObject measureCalculated = new MeasureCalculatedObject();

							bool bMdExpression = rdr.IsDBNull(0) ? false : rdr.GetBoolean(0);
							measureCalculated.reportIntervalId = rdr.GetInt32(1);
							measureCalculated.calculated = rdr.IsDBNull(2) ? false : rdr.GetBoolean(2);
							measureCalculated.aggDaily = rdr.IsDBNull(3) ? false : rdr.GetBoolean(3);
							measureCalculated.aggWeekly = rdr.IsDBNull(4) ? false : rdr.GetBoolean(4);
							measureCalculated.aggMonthly = rdr.IsDBNull(5) ? false : rdr.GetBoolean(5);
							measureCalculated.aggQuarterly = rdr.IsDBNull(6) ? false : rdr.GetBoolean(6);
							measureCalculated.aggYearly = rdr.IsDBNull(7) ? false : rdr.GetBoolean(7);
							int hId = rdr.GetInt32(8);

							if (Helper.isMeasureCalculated(bMdExpression, hId, intervalId, (long)row.MeasureID, null, measureCalculated)) {
								returnObject.error.Add(new DataImportErrorReturnObject {
									row = row.rowNumber,
									message = Resource.DI_ERR_CALCULATED
								});
							}
						}
					}
				}
			}

		}
		return null;
	}

	private ThreadStart importMeasureDataRecords(SheetDataMeasureData row, int userId) {
		string sFields = string.Empty;

		if (row.Value != null) {
			double value = Math.Round((double)row.Value, row.precision, MidpointRounding.AwayFromZero);
			sFields = sFields + " Value = " + value + ",";
		}

		if (row.Explanation != null)
			sFields = sFields + " Explanation = '" + row.Explanation + "',";

		if (row.Action != null)
			sFields = sFields + " Action = '" + row.Action + "',";

		if (string.IsNullOrEmpty(sFields))
			sFields = string.Empty;
		else
			sFields = sFields.TrimEnd().Remove(sFields.Length - 1);


		if (sFields.Trim().Length > 0) {

			string updateRowSql = "UPDATE MeasureData SET IsProcessed = 1, UserId = " + userId + ", " +
								  " LastUpdatedOn = GETDATE(), " + sFields +
								  " FROM MeasureData md INNER JOIN Measure m ON (m.Id = md.MeasureId) WHERE m.HierarchyId = " + row.HierarchyID +
								  " AND m.MeasureDefinitionId = " + row.MeasureID + " AND md.CalendarId = " + calendarId;

			string cs = Startup.ConfigurationJson.connectionString;
			using (SqlConnection con = new SqlConnection(cs)) {
				try {
					con.Open();
					SqlCommand cmd = new SqlCommand(updateRowSql, con);
					cmd.ExecuteNonQuery();
					con.Close();
				}
				catch {
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_UPLOADING });
				}
				finally { }
			}
		}

		return null;
	}

	// --------------------------------------------------------
	// Target
	// --------------------------------------------------------
	private Action validateTargetRows(SheetDataTarget row, int userId) {
		//check for null values
		if (row.MeasureID == null || row.HierarchyID == null /*|| row.Target == null*/ ) {
			if (row.MeasureID == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_MEASURE_NULL });
			if (row.HierarchyID == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });
			//if ( row.Target == null )
			//  returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_TARGET_NULL });

			return null;
		}

		//check userHierarchy
		isHierarchyValidated(row.rowNumber, (int)row.HierarchyID, null, userId);


		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string targetSQL = " SELECT md.UnitId" +
							   " FROM Measure m INNER JOIN Target t ON (t.MeasureId = m.Id) " +
							   " INNER JOIN MeasureDefinition md ON (md.Id = m.MeasureDefinitionId)" +
							   " WHERE m.MeasureDefinitionId = " + row.MeasureID +
							   " AND m.HierarchyId = " + row.HierarchyID +
							   " AND t.Active = 1";

			con.Open();

			//check Target Id and Unit Id
			SqlCommand cmd = new SqlCommand(targetSQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						if (row.Target != null) {
							if (rdr.GetInt32(0) == 1 && (row.Target < 0 || row.Target > 1))
								returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.VAL_TARGET_UNIT });
						}
						if (row.Yellow != null) {
							if (rdr.GetInt32(0) == 1 && (row.Yellow < 0 || row.Yellow > 1))
								returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.VAL_YELLOW_UNIT });
						}
					}
				}
				else {
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_TARGET_NO_EXIST });
				}
			}
		}
		return null;
	}

	private ThreadStart importTargetRecords(SheetDataTarget row, int userId) {

		string sYellow = "NULL";
		if (row.Yellow != null) {
			double yellow = Math.Round((double)row.Yellow, row.precision, MidpointRounding.AwayFromZero);
			sYellow = "" + yellow;
		}


		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();

				long measureId = -1;
				double? value = null;

				// Find MesaureId and Target Value
				string sSQL = "SELECT m.Id, t.Value FROM Measure m " +
							  " INNER JOIN Target t ON (t.MeasureId = m.Id) " +
							  " WHERE m.HierarchyId = " + row.HierarchyID +
							  " AND m.MeasureDefinitionId = " + row.MeasureID;

				SqlCommand cmd = new SqlCommand(sSQL, con);
				IAsyncResult result = cmd.BeginExecuteReader();
				using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
					if (rdr.HasRows) {
						if (rdr.Read()) {
							measureId = rdr.GetInt64(0);
							if (!rdr.IsDBNull(1)) {
								value = Math.Round((double)rdr.GetDouble(1), row.precision, MidpointRounding.AwayFromZero);
							}
						}
					}
				}

				if (measureId > 0) {

					// There should be only a target record created when a measure definition was created with a value of NULL,
					// so no need to create another one. Just update the record.
					if (value == null) {
						sSQL = "UPDATE Target SET IsProcessed = 2, Value = " + row.Target + ", YellowValue = " + sYellow + ", UserId = " + userId + ", " +
							   " LastUpdatedOn = GETDATE()" +
							   " FROM Measure m INNER JOIN Target t ON (t.MeasureId = m.Id) " +
							   " WHERE m.HierarchyId = " + row.HierarchyID +
							   " AND m.MeasureDefinitionId = " + row.MeasureID +
							   " AND t.Active = 1;";

						cmd = new SqlCommand(sSQL, con);
						cmd.ExecuteNonQuery();
					}
					// Copy existing record and set active to 0, then insert new record with the latest target and active to 1.
					else {
						sSQL = "UPDATE Target SET IsProcessed = 2, Active = 0, LastUpdatedOn = GETDATE() " +
							   " FROM Measure m INNER JOIN Target t ON (t.MeasureId = m.Id) " +
							   " WHERE m.HierarchyId = " + row.HierarchyID +
							   " AND m.MeasureDefinitionId = " + row.MeasureID +
							   " AND t.Active = 1;";

						cmd = new SqlCommand(sSQL, con);
						cmd.ExecuteNonQuery();

						sSQL = "INSERT INTO Target (MeasureId, Value, YellowValue, Active, UserId) " +
							   " VALUES (" + measureId + ", " + row.Target + ", " + sYellow + ", 1, " + userId + ");";

						cmd = new SqlCommand(sSQL, con);
						cmd.ExecuteNonQuery();
					}

				}

				con.Close();
			}
			catch {
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_UPLOADING });
			}
			finally { }
		}

		return null;
	}

	// --------------------------------------------------------
	// Customer Hierarchy
	// --------------------------------------------------------
	private Action validateCustomerRows(SheetDataCustomer row, int userId) {
		//check for null values
		if (row.HierarchyId == null || row.CalendarId == null) {
			if (row.HierarchyId == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_HIEARCHY_NULL });
			if (row.CalendarId == null)
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_CALENDAR_NULL });

			return null;
		}

		//check userHierarchy
		isHierarchyValidated(row.rowNumber, (int)row.HierarchyId, null, userId);

		//check if CalendarId exists
		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string sSQL = " SELECT Id FROM Calendar WHERE Id = " + row.CalendarId;

			con.Open();

			SqlCommand cmd = new SqlCommand(sSQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (!rdr.HasRows) {
					returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_CALENDAR_NO_EXIST });
				}
			}
		}
		return null;
	}

	private ThreadStart importCustomerRecords(SheetDataCustomer row) {
		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				//bool isFound = false;

				con.Open();


				string sSQL = "INSERT INTO CustomerHierarchy " +
							  " (HierarchyId, CalendarId, CustomerGroup, CustomerSubGroup, " +
							  " PurchaseType, TradeChannel, TradeChannelGroup, Sales, NumOrders, NumLines, " +
							  " OrderType, NumLateOrders, NumLateLines, NumOrdLens, OrdQty, IsProcessed, " +
							  " HeaderStatusCode, HeaderStatus, BlockCode, BlockText, RejectionCode, RejectionText, " +
							  " CreditStatusCheck, CreditCode) " +
							  " VALUES (@HierarchyId, @CalendarId, @CustomerGroup, @CustomerSubGroup, " +
							  " @PurchaseType, @TradeChannel, @TradeChannelGroup, @Sales, @NumOrders, @NumLines, " +
							  " @OrderType, @NumLateOrders, @NumLateLines, @NumOrdLens, @OrdQty, @IsProcessed, " +
							  " @HeaderStatusCode, @HeaderStatus, @BlockCode, @BlockText, @RejectionCode, @RejectionText, " +
							  " @CreditStatusCheck, @CreditCode);";

				SqlCommand cmd = new SqlCommand(sSQL, con);

				//string sSQL = "SELECT Id FROM CustomerHierarchy WHERE HierarchyId = @HierarchyId AND CalendarId = @CalendarId;";

				//SqlCommand  cmd = new SqlCommand(sSQL, con);
				//cmd.Parameters.AddWithValue("@HierarchyId", row.HierarchyId);
				//cmd.Parameters.AddWithValue("@CalendarId", row.CalendarId);

				//IAsyncResult result = cmd.BeginExecuteReader();
				//using (SqlDataReader rdr = cmd.EndExecuteReader(result))
				//{
				//  if (rdr.HasRows)
				//    isFound = true;
				//}

				//if ( isFound )
				//{
				//  sSQL = "UPDATE CustomerHierarchy SET " +
				//   " CustomerGroup=@CustomerGroup, CustomerSubGroup=@CustomerSubGroup, " +
				//   " PurchaseType=@PurchaseType, TradeChannel=@TradeChannel, TradeChannelGroup=@TradeChannelGroup, " +
				//   " Sales=@Sales, NumOrders=@NumOrders, NumLines=@NumLines, " +
				//   " OrderType=@OrderType, NumLateOrders=@NumLateOrders, NumLateLines=@NumLateLines, " +
				//   " NumOrdLens=@NumOrdLens, OrdQty=@OrdQty, IsProcessed=@IsProcessed, " +
				//   " HeaderStatusCode=@HeaderStatusCode, HeaderStatus=@HeaderStatus, BlockCode=@BlockCode, " +
				//   " BlockText=@BlockText, RejectionCode=@RejectionCode, RejectionText=@RejectionText, " +
				//   " CreditStatusCheck=@CreditStatusCheck, CreditCode=@CreditCode " +
				//   " WHERE HierarchyId = @HierarchyId AND CalendarId = @CalendarId;";
				//}
				//else 
				//{
				//  sSQL = "INSERT INTO CustomerHierarchy " +
				//    " (HierarchyId, CalendarId, CustomerGroup, CustomerSubGroup, " +
				//    " PurchaseType, TradeChannel, TradeChannelGroup, Sales, NumOrders, NumLines, " +
				//    " OrderType, NumLateOrders, NumLateLines, NumOrdLens, OrdQty, IsProcessed, " +
				//    " HeaderStatusCode, HeaderStatus, BlockCode, BlockText, RejectionCode, RejectionText, " +
				//    " CreditStatusCheck, CreditCode) " +
				//    " VALUES (@HierarchyId, @CalendarId, @CustomerGroup, @CustomerSubGroup, " +
				//    " @PurchaseType, @TradeChannel, @TradeChannelGroup, @Sales, @NumOrders, @NumLines, " +
				//    " @OrderType, @NumLateOrders, @NumLateLines, @NumOrdLens, @OrdQty, @IsProcessed, " +
				//    " @HeaderStatusCode, @HeaderStatus, @BlockCode, @BlockText, @RejectionCode, @RejectionText, " +
				//    " @CreditStatusCheck, @CreditCode);";
				//}

				cmd = new SqlCommand(sSQL, con);
				cmd.Parameters.AddWithValue("@HierarchyId", row.HierarchyId);
				cmd.Parameters.AddWithValue("@CalendarId", row.CalendarId);
				cmd.Parameters.AddWithValue("@CustomerGroup", (object)row.CustomerGroup ?? string.Empty);
				cmd.Parameters.AddWithValue("@CustomerSubGroup", (object)row.CustomerSubGroup ?? string.Empty);
				cmd.Parameters.AddWithValue("@PurchaseType", (object)row.PurchaseType ?? string.Empty);
				cmd.Parameters.AddWithValue("@TradeChannel", (object)row.TradeChannel ?? string.Empty);
				cmd.Parameters.AddWithValue("@TradeChannelGroup", (object)row.TradeChannelGroup ?? string.Empty);
				cmd.Parameters.AddWithValue("@Sales", (object)row.Sales ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@NumOrders", (object)row.NumOrders ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@NumLines", (object)row.NumLines ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@OrderType", (object)row.OrderType ?? string.Empty);
				cmd.Parameters.AddWithValue("@NumLateOrders", (object)row.NumLateOrders ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@NumLateLines", (object)row.NumLateLines ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@NumOrdLens", (object)row.NumOrdLens ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@OrdQty", (object)row.OrdQty ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@IsProcessed", (byte)Helper.IsProcessed.complete);
				cmd.Parameters.AddWithValue("@HeaderStatusCode", (object)row.HeaderStatusCode ?? string.Empty);
				cmd.Parameters.AddWithValue("@HeaderStatus", (object)row.HeaderStatus ?? string.Empty);
				cmd.Parameters.AddWithValue("@BlockCode", (object)row.BlockCode ?? string.Empty);
				cmd.Parameters.AddWithValue("@BlockText", (object)row.BlockText ?? string.Empty);
				cmd.Parameters.AddWithValue("@RejectionCode", (object)row.RejectionCode ?? string.Empty);
				cmd.Parameters.AddWithValue("@RejectionText", (object)row.RejectionText ?? string.Empty);
				cmd.Parameters.AddWithValue("@CreditStatusCheck", (object)row.CreditStatusCheck ?? string.Empty);
				cmd.Parameters.AddWithValue("@CreditCode", (object)row.CreditCode ?? string.Empty);

				cmd.ExecuteNonQuery();

				con.Close();
			}
			catch {
				returnObject.error.Add(new DataImportErrorReturnObject { row = row.rowNumber, message = Resource.DI_ERR_UPLOADING });
			}
			finally { }
		}
		return null;
	}
}
