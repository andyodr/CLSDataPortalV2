﻿using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Data;
using System.Timers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CLS.WebApi.Data.Models;
using CLS.WebApi.Data;
using System.Runtime.Intrinsics.Arm;
using static CLS.WebApi.Helper;
using System.Diagnostics;

namespace CLS.WebApi;

public class Helper
{
	public enum pages { measureData, target, measure, measureDefinition, hierarchy, dataImports, settings, users }
	public enum intervals { daily = 1, weekly = 2, monthly = 3, quarterly = 4, yearly = 5 };
	public enum userRoles { powerUser = 1, regionalAdministrator = 2, systemAdministrator = 3 }
	public enum dataImports { measureData = 1, target = 2, customer = 3 }
	public enum IsProcessed { no, measureData, complete }
	public static int defaultIntervalId { get; set; }
	public static int hierarchyGlobalId { get; set; }

	public static Dictionary<string, UserObject> userCookies = new Dictionary<string, UserObject>();


	public static byte? stringToByte(string boolValue) {
		if (boolValue == null)
			return null;
		else if (boolValue.ToLower() == "true")
			return 1;
		else if (boolValue.ToLower() == "false")
			return 0;
		else throw new Exception(Resource.ERR_STRING_TO_BYTE);
	}

	public static string byteToString(byte? value) {
		if (value == null)
			return "null";
		else if (value == 1)
			return "true";
		else if (value == 2)
			return "false";
		else throw new Exception(Resource.ERR_BYTE_TO_STRING);
	}

	public static bool? stringToBool(string boolValue) {
		if (boolValue == null)
			return null;
		else if (boolValue.ToLower() == "true")
			return true;
		else if (boolValue.ToLower() == "false")
			return false;
		else throw new Exception(Resource.ERR_STRING_TO_BOOL);
	}

	public static string boolToString(bool? boolValue) {
		if (boolValue == true)
			return "true";
		else if (boolValue == false)
			return "false";
		else throw new Exception(Resource.ERR_STRING_TO_STRING);
	}

	public static bool nullBoolToBool(bool? value) {
		bool result = false;
		if (value != null) {
			result = (bool)value;
		}

		return result;
	}

	public static int findIntervalId(MeasureDefinitionViewModel record, IIntervalRepository intervalRepo) {
		if (record.weekly == true)
			return intervalRepo.All().Where(i => i.Name.ToLower() == "weekly").First().Id;
		else if (record.monthly == true)
			return intervalRepo.All().Where(i => i.Name.ToLower() == "monthly").First().Id;
		else if (record.quarterly == true)
			return intervalRepo.All().Where(i => i.Name.ToLower() == "quarterly").First().Id;
		else if (record.yearly == true)
			return intervalRepo.All().Where(i => i.Name.ToLower() == "yearly").First().Id;
		else throw new Exception(String.Format(Resource.ERR_INTERVAL_ID, record.id));
	}

	internal static int FindPreviousCalendarId(DbSet<Calendar> calendarRepo, int intervalId) {
		return calendarRepo.Where(c => c.Interval.Id == intervalId && c.EndDate <= DateTime.Today).OrderByDescending(d => d.EndDate).First().Id;
	}

	internal static int findCurrentCalendarId(ICalendarRepository calendarRepo) {
		return calendarRepo.Find(c => c.IntervalId == defaultIntervalId && c.StartDate <= DateTime.Today && c.EndDate >= DateTime.Today).Id;
	}

	internal static bool addAuditTrail(string type, string code, string description, string data, DateTime lastUpdatedOn, int? userId = null) {
		string sSql = " INSERT INTO AuditTrail (Type, Code, Description, Data, UpdatedBy, LastUpdatedOn)" +
					  " VALUES (@type, @code, @description, @data, @userId, @lastUpdatedOn)";

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();
				SqlCommand cmd = new SqlCommand(sSql, con);
				cmd = new SqlCommand(sSql, con);
				cmd.Parameters.AddWithValue("@type", type);
				cmd.Parameters.AddWithValue("@code", code);
				cmd.Parameters.AddWithValue("@description", description);
				cmd.Parameters.AddWithValue("@data", data);
				cmd.Parameters.AddWithValue("@userId", userId);
				cmd.Parameters.AddWithValue("@lastUpdatedOn", lastUpdatedOn);

				cmd.ExecuteNonQuery();
				con.Close();

				return true;
			}
			catch {
				return false;
			}
		}
	}

	internal static MeasureTypeModel ErrorProcessing(Exception e, ApplicationDbContext db, HttpContext httpContext, UserObject user) {

		if (user == null) {
			httpContext.SignOutAsync("Cookies");
			return null;
		}

		int? userId = null;
		if (user != null)
			userId = user.userId;


		bool authError = false;
		string errorMessage = e.Message;

		if (errorMessage == Resource.PAGE_AUTHORIZATION_ERR) {
			authError = true;
			errorMessage = Resource.USER_NOT_AUTHORIZED;
		}

		var auditTrail = db.AuditTrail.Add(new() {
			UpdatedBy = userId,
			Type = Resource.SYSTEM,
			Code = "SE-01",
			Data = errorMessage + "\n" + e.StackTrace.ToString(),
			Description = "Web Site Error",
			LastUpdatedOn = DateTime.Now
		}
		);
		db.SaveChanges();

		var errorObject = new ErrorModel {
			id = auditTrail.Entity.Id,
			message = errorMessage,
			authError = authError
		};

		return new MeasureTypeModel() { error = errorObject };
	}

	internal static measureTypeModel errorProcessingDataImport(Exception e, IAuditTrailRepository _auditTrailRepository, int userId) {
		measureTypeModel returnModel = new measureTypeModel();
		int errorId = -1;
		string errorMessage = string.Empty;
		var record = _auditTrailRepository.Create(new AuditTrail {
			UpdatedBy = userId,
			Type = Resource.WEB_PAGES,
			Code = "WEB-06",
			Data = e.Message + "\n" + e.StackTrace.ToString(),
			Description = Resource.DATA_IMPORT,
			LastUpdatedOn = DateTime.Now
		});
		_auditTrailRepository.SaveChanges();
		errorId = (int)record.Id;
		errorMessage = e.Message;
		ErrorModel errorObject = new ErrorModel();
		errorObject.id = errorId;
		errorObject.message = errorMessage;
		returnModel.error = errorObject;
		return returnModel;
	}

	internal static bool dateIsLocked(int calendarId, int userId) {
		bool calendarBool = false;
		bool userCalendarBool = false;
		bool settingBool = false;

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {

			string calendarSQL = "select Locked from Calendar where Id =" + calendarId;
			string userCalendarSQL = "select LockOverride from UserCalendarLock where UserId = " + userId;
			string settingSQL = "select Active from Setting";
			con.Open();
			//check userHierarchy
			SqlCommand cmd = new SqlCommand(calendarSQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						if ((rdr.GetBoolean(0))) {
							calendarBool = true;
						}
					}
				}
			}
			cmd = new SqlCommand(userCalendarSQL, con);
			result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						if ((rdr.GetBoolean(0))) {
							userCalendarBool = true;
						}
					}
				}
			}
			cmd = new SqlCommand(settingSQL, con);
			result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						if ((rdr.GetBoolean(0))) {
							settingBool = true;
						}
					}
				}
			}
		}
		if (settingBool == false)
			return false;
		else if (calendarBool == true && userCalendarBool == false)
			return true;
		else return false;

	}

	internal static ICollection<RegionFilterObject> getSubsLevel(int id) {
		var children = new List<RegionFilterObject>();

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string HierarchySQL = "Select Hierarchy.Name, Hierarchy.Id from Hierarchy inner join UserHierarchy on Hierarchy.Id = UserHierarchy.HierarchyId where Hierarchy.HierarchyParentId = " + id + " AND Hierarchy.HierarchyLevelId <= 3"; //AND UserHierarchy.UserId = " + user.userId;
			string test = "Select Name, Id from Hierarchy where HierarchyParentId = " + id + " AND Hierarchy.HierarchyLevelId < 4";
			con.Open();
			//check userHierarchy
			SqlCommand cmd = new SqlCommand(test, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						RegionFilterObject current = new RegionFilterObject { hierarchy = rdr.GetString(0), id = rdr.GetInt32(1), sub = getSubsLevel(rdr.GetInt32(1)), count = 0 };
						current.count = current.sub.Count();
						children.Add(current);
					}
				}
			}
		}

		return children;
	}

	internal static List<RegionFilterObject> getSubsAll(int id) {
		List<RegionFilterObject> children = new List<RegionFilterObject>();

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string HierarchySQL = "Select Name, Id from Hierarchy where HierarchyParentId = " + id;
			con.Open();
			SqlCommand cmd = new SqlCommand(HierarchySQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows) {
					while (rdr.Read()) {
						RegionFilterObject current = new RegionFilterObject { hierarchy = rdr.GetString(0), id = rdr.GetInt32(1), sub = getSubsAll(rdr.GetInt32(1)), count = 0 };
						current.count = current.sub.Count();
						children.Add(current);
					}
				}
			}
		}

		return children;
	}

	internal static List<RegionFilterObject> GetSubs(ApplicationDbContext context, int id, UserObject user) {
		List<RegionFilterObject> children = new();
		var hierarchyList = context.Hierarchy
			.Where(h => h.HierarchyParentId == id && h.Active == true)
			.Select(h => new RegionFilterObject { hierarchy = h.Name, id = h.Id })
			.AsNoTrackingWithIdentityResolution()
			.ToList();
		foreach (var rfo in hierarchyList) {
			rfo.sub = GetSubs(context, rfo.id, user);
			rfo.count = rfo.sub.Count;
			rfo.found = user.hierarchyIds.Contains(rfo.id);
			if (rfo.found == false) {
				var child = context.Hierarchy
					.Where(c => c.HierarchyParentId == rfo.id)
					.Select(c => c.Id)
					.ToList();
				rfo.found = user.hierarchyIds.Any(i => child.Contains(i));
			}

			if (rfo.found == true) {
				children.Add(rfo);
			}
		}

		return children;
	}

	public class NullToEmptyStringResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
			return type.GetProperties()
					.Select(p => {
						var jp = base.CreateProperty(p, memberSerialization);
						jp.ValueProvider = new NullToEmptyStringValueProvider(p);
						return jp;
					}).ToList();
		}
	}

	public class NullToEmptyStringValueProvider : IValueProvider
	{
		PropertyInfo _MemberInfo;
		public NullToEmptyStringValueProvider(PropertyInfo memberInfo) {
			_MemberInfo = memberInfo;
		}

		public object GetValue(object target) {
			object result = _MemberInfo.GetValue(target);
			if (_MemberInfo.PropertyType == typeof(string) && result == null) result = "";
			return result;
		}

		public void SetValue(object target, object value) {
			_MemberInfo.SetValue(target, value);
		}
	}

	internal static bool IsMeasureCalculated(bool isCalculatedExpression, int hId, int intervalId, long measureDefId, ApplicationDbContext _context, MeasureCalculatedObject measureCalculated = null) {
		// Expression calculated overrides calculated from MeasureDefinition if true only
		if (isCalculatedExpression)
			return true;

		// If children are a rollup  
		if (haveChildrenRollup(hId, measureDefId))
			return true;

		if (measureCalculated == null) {
			var measureDef = _context.MeasureDefinition.Where(m => m.Id == measureDefId).First();
			measureCalculated = new MeasureCalculatedObject();

			measureCalculated.reportIntervalId = measureDef.ReportIntervalId;
			measureCalculated.calculated = (measureDef.Calculated == null) ? false : (bool)measureDef.Calculated;
			measureCalculated.aggDaily = (measureDef.AggDaily == null) ? false : (bool)measureDef.AggDaily;
			measureCalculated.aggWeekly = (measureDef.AggWeekly == null) ? false : (bool)measureDef.AggWeekly;
			measureCalculated.aggMonthly = (measureDef.AggMonthly == null) ? false : (bool)measureDef.AggMonthly;
			measureCalculated.aggQuarterly = (measureDef.AggQuarterly == null) ? false : (bool)measureDef.AggQuarterly;
			measureCalculated.aggYearly = (measureDef.AggYearly == null) ? false : (bool)measureDef.AggYearly;
		}

		// If Measure.Expression = 0, then check MeasureDefinition
		if (!measureCalculated.calculated) {
			if (measureCalculated.reportIntervalId == intervalId)
				return false;

			bool bReturn = false;
			// Checks aggregations from MeasureDefinition
			switch (intervalId) {
				case (int)intervals.daily:
					bReturn = measureCalculated.aggDaily;
					break;
				case (int)intervals.weekly:
					bReturn = measureCalculated.aggWeekly;
					break;
				case (int)intervals.monthly:
					bReturn = measureCalculated.aggMonthly;
					break;
				case (int)intervals.quarterly:
					bReturn = measureCalculated.aggQuarterly;
					break;
				case (int)intervals.yearly:
					bReturn = measureCalculated.aggYearly;
					break;
				default:
					break;
			}
			return bReturn;
		}
		else
			return isCalculatedExpression; // This is false
	}

	internal static bool haveChildrenRollup(int hierarchyId, long measureDefinitionId) {

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			string selectSQL =
			  " SELECT m.Id FROM Measure m " +
			  " INNER JOIN Hierarchy h ON (h.Id = m.HierarchyId) " +
			  " WHERE h.HierarchyParentId = " + hierarchyId +
			  " AND m.MeasureDefinitionId = " + measureDefinitionId +
			  " AND m.Active= 1 " +
			  " AND m.Rollup = 1 ";

			con.Open();
			SqlCommand cmd = new SqlCommand(selectSQL, con);
			IAsyncResult result = cmd.BeginExecuteReader();
			using (SqlDataReader rdr = cmd.EndExecuteReader(result)) {
				if (rdr.HasRows)
					return true;
			}
		}
		return false;
	}

	internal static DataImportObject dataImportHeading(dataImports dataImport) {
		DataImportObject result = new DataImportObject { heading = new List<headingObject>() };

		if (dataImport == dataImports.target) {
			result.id = (int)dataImports.target;
			result.name = "Target";
			result.heading.Add(new headingObject { title = "hierarchyid", required = true });
			result.heading.Add(new headingObject { title = "measureid", required = true });
			result.heading.Add(new headingObject { title = "target", required = true });
			result.heading.Add(new headingObject { title = "yellow", required = false });
		}
		else if (dataImport == dataImports.customer) {
			result.id = (int)dataImports.customer;
			result.name = "Customer Region";
			result.heading.Add(new headingObject { title = "hierarchyid", required = true });
			result.heading.Add(new headingObject { title = "calendarid", required = true });
			result.heading.Add(new headingObject { title = "customergroup", required = false });
			result.heading.Add(new headingObject { title = "customersubgroup", required = false });
			result.heading.Add(new headingObject { title = "purchasetype", required = false });
			result.heading.Add(new headingObject { title = "tradechannel", required = false });
			result.heading.Add(new headingObject { title = "tradechannelgroup", required = false });
			result.heading.Add(new headingObject { title = "sales", required = false });
			result.heading.Add(new headingObject { title = "numorders", required = false });
			result.heading.Add(new headingObject { title = "numlines", required = false });
			result.heading.Add(new headingObject { title = "ordertype", required = false });
			result.heading.Add(new headingObject { title = "numlateorders", required = false });
			result.heading.Add(new headingObject { title = "numlatelines", required = false });
			result.heading.Add(new headingObject { title = "numordlens", required = false });
			result.heading.Add(new headingObject { title = "ordqty", required = false });
			result.heading.Add(new headingObject { title = "headerstatuscode", required = false });
			result.heading.Add(new headingObject { title = "headerstatus", required = false });
			result.heading.Add(new headingObject { title = "blockcode", required = false });
			result.heading.Add(new headingObject { title = "blocktext", required = false });
			result.heading.Add(new headingObject { title = "rejectioncode", required = false });
			result.heading.Add(new headingObject { title = "rejectiontext", required = false });
			result.heading.Add(new headingObject { title = "creditstatuscheck", required = false });
			result.heading.Add(new headingObject { title = "creditcode", required = false });
		}
		else {
			result.id = (int)dataImports.measureData;
			result.name = "Measure Data";
			result.heading.Add(new headingObject { title = "hierarchyid", required = true });
			result.heading.Add(new headingObject { title = "measureid", required = true });
			result.heading.Add(new headingObject { title = "value", required = true });
			result.heading.Add(new headingObject { title = "explanation", required = false });
			result.heading.Add(new headingObject { title = "action", required = false });
		}

		return result;
	}

	internal static bool canEditValueFromSpecialHierarchy(int hierarchyId) {
		//this is for a special case where some level 2 hierarchies can not be edited since they are a sum value
		bool result = true;
		if (Startup.ConfigurationJson.specialHierarhies != null) {
			if (Startup.ConfigurationJson.specialHierarhies.Contains(hierarchyId))
				result = false;
		}

		return result;
	}

	internal static string CreateMeasuresAndTargets(ApplicationDbContext dbc, int userId, MeasureDefinitionViewModel measureDef) {
		try {
			string result = String.Empty;
			var hierarchyRecords = from record in dbc.Hierarchy
								   select new { id = record.Id };
			var dtNow = DateTime.Now;
			foreach (var id in hierarchyRecords) {
				//create Measure records
				_ = dbc.Measure.Add(new() {
					HierarchyId = id.id,
					MeasureDefinitionId = measureDef.id ?? -1,
					Active = true,
					Expression = measureDef.calculated,
					Rollup = true,
					LastUpdatedOn = dtNow
				});
			}

			var measures = from measure in dbc.Measure
							 where measure.MeasureDefinitionId == measureDef.id
							 select new { id = measure.Id };
			//make target ids
			foreach (var measure in measures) {
				_ = dbc.Target.Add(new() {
					MeasureId = measure.id,
					Active = true,
					UserId = userId,
					IsProcessed = 2,
					LastUpdatedOn = dtNow
				});
			}

			return result;
		}
		catch (Exception e) {
			return e.Message;
		}
	}

	internal static string CreateMeasuresAndTargets(ApplicationDbContext dbc, int userId, int hierarchyId) {
		try {
			string result = String.Empty;
			var dtNow = DateTime.Now;
			foreach (var measureDef in dbc.MeasureDefinition.Select(md => new { md.Id, md.Calculated })) {
				//create Measure records
				_ = dbc.Measure.Add(new() {
					HierarchyId = hierarchyId,
					MeasureDefinitionId = measureDef.Id,
					Active = true,
					Expression = measureDef.Calculated,
					Rollup = true,
					LastUpdatedOn = dtNow
				});
			}

			//make target ids
			foreach (var measure in dbc.Measure.Where(m => m.HierarchyId == hierarchyId)) {
				_ = dbc.Target.Add(new() {
					Measure = measure,
					Active = true,
					UserId = userId,
					IsProcessed = (byte)IsProcessed.complete,
					LastUpdatedOn = dtNow
				});
			}

			return result;
		}
		catch (Exception e) {
			return e.Message;
		}
	}

	public struct DateTimeSpan
	{
		private readonly int years;
		private readonly int months;
		private readonly int days;
		private readonly int hours;
		private readonly int minutes;
		private readonly int seconds;
		private readonly int milliseconds;

		public DateTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds) {
			this.years = years;
			this.months = months;
			this.days = days;
			this.hours = hours;
			this.minutes = minutes;
			this.seconds = seconds;
			this.milliseconds = milliseconds;
		}

		public int Years { get { return years; } }
		public int Months { get { return months; } }
		public int Days { get { return days; } }
		public int Hours { get { return hours; } }
		public int Minutes { get { return minutes; } }
		public int Seconds { get { return seconds; } }
		public int Milliseconds { get { return milliseconds; } }

		enum Phase { Years, Months, Days, Done }

		public static DateTimeSpan CompareDates(DateTime date1, DateTime date2) {
			if (date2 < date1) {
				var sub = date1;
				date1 = date2;
				date2 = sub;
			}

			DateTime current = date1;
			int years = 0;
			int months = 0;
			int days = 0;

			Phase phase = Phase.Years;
			DateTimeSpan span = new DateTimeSpan();
			int officialDay = current.Day;

			while (phase != Phase.Done) {
				switch (phase) {
					case Phase.Years:
						if (current.AddYears(years + 1) > date2) {
							phase = Phase.Months;
							current = current.AddYears(years);
						}
						else {
							years++;
						}
						break;
					case Phase.Months:
						if (current.AddMonths(months + 1) > date2) {
							phase = Phase.Days;
							current = current.AddMonths(months);
							if (current.Day < officialDay && officialDay <= DateTime.DaysInMonth(current.Year, current.Month))
								current = current.AddDays(officialDay - current.Day);
						}
						else {
							months++;
						}
						break;
					case Phase.Days:
						if (current.AddDays(days + 1) > date2) {
							current = current.AddDays(days);
							var timespan = date2 - current;
							span = new DateTimeSpan(years, months, days, timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
							phase = Phase.Done;
						}
						else {
							days++;
						}
						break;
				}
			}
			return span;
		}

	}

	public static UpdatedObject LastUpdatedOnObj(DateTime lastUpdatedOn, string? userName) {

		var update = new UpdatedObject();
		update.by = userName;
		update.longDt = lastUpdatedOn.ToString();

		//// Convert to seconds
		//int seconds = (int)DateTime.Now.Subtract(lastUpdatedOn).TotalSeconds;

		//// Less than a minute
		//if (seconds < 60)
		//{
		//  update.shortDt = seconds.ToString() + " secs";
		//  return update;
		//}
		//// Less than an hour
		//if (seconds < 3600)
		//{
		//  update.shortDt = ((int)DateTime.Now.Subtract(lastUpdatedOn).TotalMinutes).ToString() + " mins";
		//  return update;
		//}

		// It gets more complex. Gets days, months, years
		var dateSpan = DateTimeSpan.CompareDates(lastUpdatedOn, DateTime.Now);

		// Less than a year
		if (dateSpan.Years == 0) {
			// Less than a month
			if (dateSpan.Months == 0) {
				// Less than a day
				if (dateSpan.Days == 0) {
					// Less than an hour
					if (dateSpan.Hours == 0) {
						// Less than a minute
						if (dateSpan.Minutes == 0)
							update.shortDt = dateSpan.Seconds.ToString() + " secs";
						else
							update.shortDt = dateSpan.Minutes.ToString() + " mins";
					}
					else
						update.shortDt = dateSpan.Hours.ToString() + " hours " + dateSpan.Minutes.ToString() + " mins";
				}
				else
					update.shortDt = dateSpan.Days.ToString() + " days " + dateSpan.Hours.ToString() + " hours";
			}
			else
				update.shortDt = dateSpan.Months.ToString() + " months " + dateSpan.Days.ToString() + " days";

			return update;
		}

		// Over a year
		update.shortDt = dateSpan.Years.ToString() + " years " + dateSpan.Months.ToString() + " months";

		return update;
	}

	public static UserObject? GetUserObject(ApplicationDbContext dbc, string userName) {
		try {
			var entity = dbc.User
				.Where(u => u.UserName == userName)
				.Include(u => u.UserRole)
				.Include(u => u.UserCalendarLocks)
				.Include(u => u.UserHierarchies)
				.AsNoTrackingWithIdentityResolution().Single();
			var localUser = new UserObject {
					userId = entity.Id,
					userRoleId = entity.UserRole!.Id,
					userName = entity.UserName,
					firstName = entity.FirstName,
					userRole = entity.UserRole.Name };
			localUser.calendarLockIds.AddRange(entity.UserCalendarLocks!.Select(c => new UserCalendarLocks {
				CalendarId = c.CalendarId,
				LockOverride = c.LockOverride
			}));
			localUser.hierarchyIds.AddRange(entity.UserHierarchies!.Select(h => h.Id));

			// Sets page authorization based on roles
			localUser.Authorized[pages.measureData] = true;
			localUser.Authorized[pages.target] = (localUser.userRoleId > (int)userRoles.powerUser);
			localUser.Authorized[pages.measure] = (localUser.userRoleId == (int)userRoles.systemAdministrator);
			localUser.Authorized[pages.measureDefinition] = (localUser.userRoleId > (int)userRoles.powerUser);
			localUser.Authorized[pages.hierarchy] = (localUser.userRoleId == (int)userRoles.systemAdministrator);
			localUser.Authorized[pages.dataImports] = true;
			localUser.Authorized[pages.settings] = (localUser.userRoleId == (int)userRoles.systemAdministrator);
			localUser.Authorized[pages.users] = (localUser.userRoleId == (int)userRoles.systemAdministrator);

			return localUser;
		}
		catch {
			return null;
		}
	}

	public static bool IsUserPageAuthorized(pages page, int roleId) {

		bool bReturn = false;
		switch (page) {
			case pages.measureData:
				bReturn = true;
				break;
			case pages.target:
				bReturn = (roleId > (int)userRoles.powerUser);
				break;
			case pages.measure:
				bReturn = (roleId == (int)userRoles.systemAdministrator);
				break;
			case pages.measureDefinition:
				bReturn = (roleId > (int)userRoles.powerUser);
				break;
			case pages.hierarchy:
				bReturn = (roleId == (int)userRoles.systemAdministrator);
				break;
			case pages.dataImports:
				bReturn = true;
				break;
			case pages.settings:
				bReturn = (roleId == (int)userRoles.systemAdministrator);
				break;
			case pages.users:
				bReturn = (roleId == (int)userRoles.systemAdministrator);
				break;
			default:
				break;
		}
		return bReturn;
	}

	public static UserObject? UserAuthorization(ClaimsPrincipal userClaim) {
		if (!userClaim.Identity.IsAuthenticated) {
			return null;
		}

		var userId = userClaim.Claims.Where(c => c.Type == "userId").FirstOrDefault();
		if (userId == null) {
			return null;
		}

		if (!userCookies.ContainsKey(userId.Value)) {
			return null;
		}

		return userCookies[userId.Value];
	}

	internal static void UserDeleteHierarchy(int userId, ApplicationDbContext context) {
		var deleteRecords = context.UserHierarchy.Where(u => u.User.Id == userId).ToList();
		if (deleteRecords.Count > 0) {
			foreach (var record in deleteRecords) {
				context.UserHierarchy.Remove(record);
			}
		}
	}

	internal static void AddUserHierarchy(int userId, ApplicationDbContext context, List<int> hierarchiesId, List<int> addedHierarchies) {
		foreach (int hId in hierarchiesId) {
			if (!addedHierarchies.Contains(hId) && !context.UserHierarchy.Where(u => u.Hierarchy!.Id == hId && u.User.Id == userId).Any()) {
				_ = context.UserHierarchy.Add(new() {
					LastUpdatedOn = DateTime.Now,
					HierarchyId = hId,
					UserId = userId
				});
				AddHierarchyChildren(userId, context, hId, addedHierarchies);
				addedHierarchies.Add(hId);
			}
		}
	}

	internal static void AddHierarchyChildren(int userId, ApplicationDbContext context, int hierarchyId, List<int> addedHierarchies) {
		List<RegionFilterObject> children = new();
		var hierarchies = context.Hierarchy.Where(h => h.HierarchyParentId == hierarchyId).ToList();

		foreach (var record in hierarchies) {
			if (!addedHierarchies.Contains(record.Id) && !context.UserHierarchy.Where(u => u.Hierarchy!.Id == record.Id && u.User.Id == userId).Any()) {
				_ = context.UserHierarchy.Add(new() {
					LastUpdatedOn = DateTime.Now,
					HierarchyId = record.Id,
					UserId = userId
				});
				addedHierarchies.Add(record.Id);
				AddHierarchyChildren(userId, context, record.Id, addedHierarchies);
			}
		}
	}

	internal static bool IsDataLocked(int calendarId, int userId, Calendar calendar, ApplicationDbContext db) {

		// --------------------------------- Lock Override ----------------------------
		bool isLocked = false;
		bool isLockedOverride = false;

		if (calendar.IntervalId == (int)Helper.intervals.monthly) {
			if (calendar.Locked == true) {
				isLocked = true;
				var userCal = db.UserCalendarLock.Where(u => u.UserId == userId && u.CalendarId == calendarId);
				foreach (var itemUserCal in userCal) {
					if ((bool)itemUserCal.LockOverride) {
						isLockedOverride = true;
						break;
					}
				}
			}
		}

		// This is a fix because Settings page does not have calendarLock by other intervals yet. Only monthly.

		if (calendar.IntervalId == (int)Helper.intervals.weekly) {
			var cal = db.Calendar.Where(
			  c => c.IntervalId == (int)Helper.intervals.monthly && c.Year == calendar.Year && c.StartDate >= calendar.StartDate && c.EndDate <= calendar.StartDate);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = db.UserCalendarLock.Where(u => u.UserId == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if ((bool)itemUserCal.LockOverride) {
							isLockedOverride = true;
							break;
						}
					}
				}
			}
		}
		if (calendar.IntervalId == (int)Helper.intervals.quarterly) {
			var cal = db.Calendar.Where(c => c.IntervalId == (int)Helper.intervals.monthly && c.Year == calendar.Year && c.Quarter == calendar.Quarter);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = userCalendarLockRepo.All().Where(u => u.UserId == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if ((bool)itemUserCal.LockOverride) {
							isLockedOverride = true;
							break;
						}
					}
				}
			}
		}
		if (calendar.IntervalId == (int)Helper.intervals.yearly) {
			var cal = db.Calendar.Where(c => c.IntervalId == (int)Helper.intervals.monthly && c.Year == calendar.Year);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = db.UserCalendarLock.Where(u => u.UserId == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if ((bool)itemUserCal.LockOverride) {
							isLockedOverride = true;
							break;
						}
					}
				}
			}
		}

		if (isLocked && !isLockedOverride)
			isLocked = true;
		else
			isLocked = false;

		return isLocked;
	}

	internal static int? CalculateScheduleInt(string? value, string pattern1, string pattern2) {
		if (value == null) { return null; }
		int? iReturn = 0;
		try {
			String[] schedule = Regex.Split(value, pattern2);
			if (schedule.Length == 3) {
				switch (pattern1) {
					case "HH":
						iReturn = Int32.Parse(schedule[0]);
						break;
					case "MM":
						iReturn = Int32.Parse(schedule[1]);
						break;
					case "SS":
						iReturn = Int32.Parse(schedule[2]);
						break;
					default:
						break;
				}
			}

			return iReturn;
		}
		catch (Exception) {
			return iReturn;
		}
	}

	internal static string CalculateSchedule(int? HH, int? MM, int? SS) {
		string sReturn = "00:01:00";
		try {
			string sHH = "00";
			string sMM = "01";
			string sSS = "00";

			if (HH != null) sHH = string.Format("{0:00}", HH);
			if (MM != null) sMM = string.Format("{0:00}", MM);
			if (SS != null) sSS = string.Format("{0:00}", SS);

			return sHH + ":" + sMM + ":" + sSS;
		}
		catch (Exception) {
			return sReturn;
		}
	}

	internal static string CalculateScheduleStr(string value, string pattern1, string pattern2) {
		string sReturn = "00";
		try {
			String[] schedule = Regex.Split(value, pattern2);
			if (schedule.Count() == 3) {
				switch (pattern1) {
					case "HH":
						sReturn = schedule[0];
						break;
					case "MM":
						sReturn = schedule[1];
						break;
					case "SS":
						sReturn = schedule[2];
						break;
					default:
						break;
				}
			}
			return sReturn;
		}
		catch (Exception) {
			return sReturn;
		}
	}

	internal static bool UpdateMeasureDataIsProcessed(long measureDefId, int userId) {

		DateTime lastUpdatedOn = DateTime.Now;

		string updateRowSql = "UPDATE md SET md.IsProcessed = @isProcessed, md.UserId = @userId, md.LastUpdatedOn = @lastUpdatedOn" +
							  " FROM MeasureData md" +
							  " INNER JOIN Measure m ON (m.Id = md.MeasureId)" +
							  " WHERE m.MeasureDefinitionId = @measureDefId";

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();
				SqlCommand cmd = new SqlCommand(updateRowSql, con);
				cmd = new SqlCommand(updateRowSql, con);
				cmd.Parameters.AddWithValue("@isProcessed", (byte)Helper.IsProcessed.measureData);
				cmd.Parameters.AddWithValue("@userId", userId);
				cmd.Parameters.AddWithValue("@lastUpdatedOn", lastUpdatedOn);
				cmd.Parameters.AddWithValue("@measureDefId", measureDefId);

				cmd.ExecuteNonQuery();
				con.Close();

				return true;
			}
			catch {
				return false;
			}
		}
	}

	internal static bool UpdateMeasureDataIsProcessed(long measureId, int userId, DateTime lastUpdatedOn, Helper.IsProcessed isProcessed) {

		string updateRowSql = "UPDATE MeasureData SET IsProcessed = @isProcessed, UserId = @userId, LastUpdatedOn = @lastUpdatedOn " +
							  " FROM MeasureData WHERE MeasureId = @measureId";

		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();
				SqlCommand cmd = new SqlCommand(updateRowSql, con);
				cmd = new SqlCommand(updateRowSql, con);
				cmd.Parameters.AddWithValue("@isProcessed", (byte)isProcessed);
				cmd.Parameters.AddWithValue("@userId", userId);
				cmd.Parameters.AddWithValue("@lastUpdatedOn", lastUpdatedOn);
				cmd.Parameters.AddWithValue("@measureId", measureId);

				cmd.ExecuteNonQuery();
				con.Close();

				return true;
			}
			catch {
				return false;
			}
		}
	}

	internal static bool CreateMeasureDataRecords(int intervalId, long? measureDefIf = null) {

		// Create measure dtaa records if don't exist
		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();
				SqlCommand cmd = new SqlCommand("spMeasureData", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@p_interval", intervalId);
				cmd.Parameters.AddWithValue("@p_days_offset", 0);
				cmd.Parameters.AddWithValue("@p_measureDefId", (object)measureDefIf ?? DBNull.Value);

				cmd.ExecuteNonQuery();
				con.Close();

				return true;
			}
			catch {
				return false;
			}
		}
	}

	internal static bool StartSQLJob(string spName) {

		// Create measure dtaa records if don't exist
		string cs = Startup.ConfigurationJson.connectionString;
		using (SqlConnection con = new SqlConnection(cs)) {
			try {
				con.Open();
				SqlCommand cmd = new SqlCommand("msdb.dbo.sp_start_job", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@job_name", spName);

				cmd.ExecuteNonQuery();
				con.Close();

				return true;
			}
			catch {
				return false;
			}
		}
	}

	internal static Task UseCookieAuthenticationValidate(CookieValidatePrincipalContext context) {
		try {
			string userId = context.Principal.Claims.Where(c => c.Type == "userId").First().Value;
			if (userCookies.ContainsKey(userId))
				userCookies[userId].expiresUtc = context.Properties.ExpiresUtc;
		}
		catch (Exception) {
		}
		return Task.FromResult(0);
	}

	public static void OnUsersTimedEvent(object source, ElapsedEventArgs e) {
		try {

			if (userCookies.Count() > 0) {
				var keysToRemove = userCookies.Where(c => c.Value.expiresUtc == null ||
													(c.Value.expiresUtc != null && c.Value.expiresUtc.Value.UtcTicks < DateTimeOffset.UtcNow.UtcTicks))
								  .Select(c => c.Key)
								  .ToArray();

				foreach (var key in keysToRemove) {
					if (userCookies.ContainsKey(key)) {
						addAuditTrail(
						  Resource.SECURITY,
						   "SEC-02",
						   "Logout",
						   @"Timeout Logout / ID=" + key + " / Username=" + userCookies[key].userName,
						   DateTime.Now,
						   Convert.ToInt32(key)
						);
						userCookies.Remove(key);
					}
				}
			}
		}
		catch (Exception) {
		}
	}
}