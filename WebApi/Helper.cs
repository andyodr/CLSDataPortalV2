using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using CLS.WebApi.Data.Models;
using CLS.WebApi.Data;

namespace CLS.WebApi;

public static class Helper
{
	public enum pages { measureData, target, measure, measureDefinition, hierarchy, dataImports, settings, users }

	public enum Intervals { Daily = 1, Weekly = 2, Monthly = 3, Quarterly = 4, Yearly = 5 };

	public enum Roles { PowerUser = 1, RegionalAdministrator = 2, SystemAdministrator = 3 }

	public enum dataImports { measureData = 1, target = 2, customer = 3 }

	public enum IsProcessed { no, measureData, complete }

	public static int hierarchyGlobalId { get; set; } = 1;

	public static Dictionary<string, UserObject> userCookies = new Dictionary<string, UserObject>();

	public static byte? stringToByte(string boolValue) {
		if (boolValue is null)
			return null;
		else if (boolValue.ToLower() == "true")
			return 1;
		else if (boolValue.ToLower() == "false")
			return 0;
		else throw new Exception(Resource.ERR_STRING_TO_BYTE);
	}

	public static string byteToString(byte? value) {
		if (value is null)
			return "null";
		else if (value == 1)
			return "true";
		else if (value == 2)
			return "false";
		else throw new Exception(Resource.ERR_BYTE_TO_STRING);
	}

	internal static int FindPreviousCalendarId(DbSet<Calendar> calendarRepo, int intervalId) {
		return calendarRepo.Where(c => c.Interval.Id == intervalId && c.EndDate <= DateTime.Today).OrderByDescending(d => d.EndDate).First().Id;
	}

	internal static bool AddAuditTrail(ApplicationDbContext dbc, string type, string code, string description, string data, DateTime lastUpdatedOn, int? userId = null) {
		try {
			_ = dbc.AuditTrail.Add(new AuditTrail {
				Type = type,
				Code = code,
				Description = description,
				Data = data,
				UpdatedBy = userId,
				LastUpdatedOn = lastUpdatedOn
			});
			dbc.SaveChanges();

			return true;
		}
		catch {
			return false;
		}
	}

	internal static ErrorModel ErrorProcessing(ApplicationDbContext db, Exception e, int? userId) {
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
			Data = errorMessage + "\n" + e.StackTrace?.ToString(),
			Description = "Web Site Error",
			LastUpdatedOn = DateTime.Now
		}
		);
		db.SaveChanges();

		return new ErrorModel {
			Id = auditTrail.Entity.Id,
			Message = errorMessage,
			AuthError = authError
		};
	}

	internal static ICollection<RegionFilterObject> GetSubsLevel(ApplicationDbContext dbc, int id) {
		var children = dbc.Hierarchy
			.Where(h => h.HierarchyParentId == id && h.HierarchyLevelId < 4)
			.Select(h => new RegionFilterObject { Hierarchy = h.Name, Id = h.Id })
			.AsNoTrackingWithIdentityResolution()
			.ToArray();
		foreach (var rfo in children) {
			rfo.Sub = GetSubsLevel(dbc, rfo.Id);
			rfo.Count = rfo.Sub.Count;
		}

		return children;
	}

	internal static List<RegionFilterObject> GetSubs(ApplicationDbContext dbc, int id, UserObject user) {
		var children = new List<RegionFilterObject>();
		var hierarchyList = dbc.Hierarchy
			.Where(h => h.HierarchyParentId == id && h.Active == true)
			.Select(h => new RegionFilterObject { Hierarchy = h.Name, Id = h.Id })
			.AsNoTrackingWithIdentityResolution()
			.ToArray();
		foreach (var rfo in hierarchyList) {
			rfo.Sub = GetSubs(dbc, rfo.Id, user);
			rfo.Count = rfo.Sub.Count;
			rfo.Found = user.hierarchyIds.Contains(rfo.Id);
			if (rfo.Found == false) {
				var child = dbc.Hierarchy
					.Where(c => c.HierarchyParentId == rfo.Id)
					.Select(c => c.Id)
					.Distinct()
					.ToArray();
				rfo.Found = user.hierarchyIds.Any(i => child.Contains(i));
			}

			if (rfo.Found == true) {
				children.Add(rfo);
			}
		}

		return children;
	}

	internal static bool IsMeasureCalculated(ApplicationDbContext dbc, bool isCalculatedExpression, int hId, int intervalId, long measureDefId, MeasureCalculatedObject? measureCalculated = null) {
		// Expression calculated overrides calculated from MeasureDefinition if true only
		if (isCalculatedExpression) {
			return true;
		}

		// If children are a rollup
		if (dbc.Measure.Where(m => m.MeasureDefinitionId == measureDefId
				&& m.Hierarchy!.HierarchyParentId == hId && m.Active == true && m.Rollup == true).Any()) {
			return true;
		}

		if (measureCalculated is null) {
			var measureDef = dbc.MeasureDefinition.Where(m => m.Id == measureDefId).FirstOrDefault();
			measureCalculated = new MeasureCalculatedObject {
				reportIntervalId = measureDef?.ReportIntervalId ?? 0,
				calculated = measureDef?.Calculated ?? false,
				aggDaily = measureDef?.AggDaily ?? false,
				aggWeekly = measureDef?.AggWeekly ?? false,
				aggMonthly = measureDef?.AggMonthly ?? false,
				aggQuarterly = measureDef?.AggQuarterly ?? false,
				aggYearly = measureDef?.AggYearly ?? false
			};
		}

		// If Measure.Expression = 0, then check MeasureDefinition
		if (measureCalculated.calculated) {
			return isCalculatedExpression; // This is false
		}
		else {
			if (measureCalculated.reportIntervalId == intervalId) {
				return false;
			}

			bool bReturn = false;
			// Checks aggregations from MeasureDefinition
			switch (intervalId) {
				case (int)Intervals.Daily:
					bReturn = measureCalculated.aggDaily;
					break;
				case (int)Intervals.Weekly:
					bReturn = measureCalculated.aggWeekly;
					break;
				case (int)Intervals.Monthly:
					bReturn = measureCalculated.aggMonthly;
					break;
				case (int)Intervals.Quarterly:
					bReturn = measureCalculated.aggQuarterly;
					break;
				case (int)Intervals.Yearly:
					bReturn = measureCalculated.aggYearly;
					break;
				default:
					break;
			}
			return bReturn;
		}
	}

	internal static DataImportObject DataImportHeading(dataImports dataImport) {
		DataImportObject result = new() { Heading = new List<HeadingObject>() };

		if (dataImport == dataImports.target) {
			result.Id = (int)dataImports.target;
			result.Name = "Target";
			result.Heading.Add(new HeadingObject { Title = "hierarchyid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "measureid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "target", Required = true });
			result.Heading.Add(new HeadingObject { Title = "yellow", Required = false });
		}
		else if (dataImport == dataImports.customer) {
			result.Id = (int)dataImports.customer;
			result.Name = "Customer Region";
			result.Heading.Add(new HeadingObject { Title = "hierarchyid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "calendarid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "customergroup", Required = false });
			result.Heading.Add(new HeadingObject { Title = "customersubgroup", Required = false });
			result.Heading.Add(new HeadingObject { Title = "purchasetype", Required = false });
			result.Heading.Add(new HeadingObject { Title = "tradechannel", Required = false });
			result.Heading.Add(new HeadingObject { Title = "tradechannelgroup", Required = false });
			result.Heading.Add(new HeadingObject { Title = "sales", Required = false });
			result.Heading.Add(new HeadingObject { Title = "numorders", Required = false });
			result.Heading.Add(new HeadingObject { Title = "numlines", Required = false });
			result.Heading.Add(new HeadingObject { Title = "ordertype", Required = false });
			result.Heading.Add(new HeadingObject { Title = "numlateorders", Required = false });
			result.Heading.Add(new HeadingObject { Title = "numlatelines", Required = false });
			result.Heading.Add(new HeadingObject { Title = "numordlens", Required = false });
			result.Heading.Add(new HeadingObject { Title = "ordqty", Required = false });
			result.Heading.Add(new HeadingObject { Title = "headerstatuscode", Required = false });
			result.Heading.Add(new HeadingObject { Title = "headerstatus", Required = false });
			result.Heading.Add(new HeadingObject { Title = "blockcode", Required = false });
			result.Heading.Add(new HeadingObject { Title = "blocktext", Required = false });
			result.Heading.Add(new HeadingObject { Title = "rejectioncode", Required = false });
			result.Heading.Add(new HeadingObject { Title = "rejectiontext", Required = false });
			result.Heading.Add(new HeadingObject { Title = "creditstatuscheck", Required = false });
			result.Heading.Add(new HeadingObject { Title = "creditcode", Required = false });
		}
		else {
			result.Id = (int)dataImports.measureData;
			result.Name = "Measure Data";
			result.Heading.Add(new HeadingObject { Title = "hierarchyid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "measureid", Required = true });
			result.Heading.Add(new HeadingObject { Title = "value", Required = true });
			result.Heading.Add(new HeadingObject { Title = "explanation", Required = false });
			result.Heading.Add(new HeadingObject { Title = "action", Required = false });
		}

		return result;
	}

	internal static bool CanEditValueFromSpecialHierarchy(ConfigurationObject config, int hierarchyId) {
		//this is for a special case where some level 2 hierarchies can not be edited since they are a sum value
		bool result = true;
		if (config.specialHierarhies is not null) {
			if (config.specialHierarhies.Contains(hierarchyId)) {
				result = false;
			}
		}

		return result;
	}

	internal static string CreateMeasuresAndTargets(ApplicationDbContext dbc, int userId, MeasureDefinitionEdit measureDef) {
		try {
			string result = String.Empty;
			var hierarchyRecords = from record in dbc.Hierarchy
								   select new { id = record.Id };
			var dtNow = DateTime.Now;
			foreach (var id in hierarchyRecords) {
				//create Measure records
				_ = dbc.Measure.Add(new() {
					HierarchyId = id.id,
					MeasureDefinitionId = measureDef.Id,
					Active = true,
					Expression = measureDef.Calculated,
					Rollup = true,
					LastUpdatedOn = dtNow
				});
			}

			var measures = from measure in dbc.Measure
						   where measure.MeasureDefinitionId == measureDef.Id
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
		update.By = userName;
		update.LongDt = lastUpdatedOn.ToString();

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
							update.ShortDt = dateSpan.Seconds.ToString() + " secs";
						else
							update.ShortDt = dateSpan.Minutes.ToString() + " mins";
					}
					else
						update.ShortDt = dateSpan.Hours.ToString() + " hours " + dateSpan.Minutes.ToString() + " mins";
				}
				else
					update.ShortDt = dateSpan.Days.ToString() + " days " + dateSpan.Hours.ToString() + " hours";
			}
			else
				update.ShortDt = dateSpan.Months.ToString() + " months " + dateSpan.Days.ToString() + " days";

			return update;
		}

		// Over a year
		update.ShortDt = dateSpan.Years.ToString() + " years " + dateSpan.Months.ToString() + " months";

		return update;
	}

	public static UserObject? CreateUserObject(ClaimsPrincipal userClaim) {
		var claimUserId = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (claimUserId is string userId) {
			return new UserObject {
				Id = int.Parse(userId),
				UserName = userClaim.Identity!.Name!
			};
		}

		return null;
	}

	internal static bool IsDataLocked(int calendarId, int userId, Calendar calendar, ApplicationDbContext dbc) {
		// --------------------------------- Lock Override ----------------------------
		bool isLocked = false;
		bool isLockedOverride = false;

		if (calendar.Interval.Id == (int)Intervals.Monthly) {
			if (calendar.Locked == true) {
				isLocked = true;
				var userCal = dbc.UserCalendarLock.Where(u => u.User.Id == userId && u.CalendarId == calendarId);
				foreach (var itemUserCal in userCal) {
					if (itemUserCal.LockOverride ?? false) {
						isLockedOverride = true;
						break;
					}
				}
			}
		}

		// This is a fix because Settings page does not have calendarLock by other intervals yet. Only monthly.
		if (calendar.Interval.Id == (int)Intervals.Weekly) {
			var cal = dbc.Calendar.Where(
			  c => c.Interval.Id == (int)Intervals.Monthly && c.Year == calendar.Year && c.StartDate >= calendar.StartDate && c.EndDate <= calendar.StartDate);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = dbc.UserCalendarLock.Where(u => u.User.Id == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if (itemUserCal.LockOverride ?? false) {
							isLockedOverride = true;
							break;
						}
					}
				}
			}
		}
		if (calendar.Interval.Id == (int)Intervals.Quarterly) {
			var cal = dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Monthly && c.Year == calendar.Year && c.Quarter == calendar.Quarter);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = dbc.UserCalendarLock.Where(u => u.User.Id == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if (itemUserCal.LockOverride ?? false) {
							isLockedOverride = true;
							break;
						}
					}
				}
			}
		}
		if (calendar.Interval.Id == (int)Intervals.Yearly) {
			var cal = dbc.Calendar.Where(c => c.Interval.Id == (int)Intervals.Monthly && c.Year == calendar.Year);
			foreach (var item in cal) {
				if (item.Locked == true) {
					isLocked = true;
					var userCal = dbc.UserCalendarLock.Where(u => u.User.Id == userId && u.CalendarId == item.Id);
					foreach (var itemUserCal in userCal) {
						if (itemUserCal.LockOverride ?? false) {
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
		if (value is null) { return null; }
		int? iReturn = 0;
		try {
			string[] schedule = Regex.Split(value, pattern2);
			if (schedule.Length == 3) {
				return pattern1 switch {
					"HH" => int.Parse(schedule[0]),
					"MM" => int.Parse(schedule[1]),
					"SS" => int.Parse(schedule[2]),
					_ => iReturn
				};
			}

			return iReturn;
		}
		catch (Exception) {
			return iReturn;
		}
	}

	internal static string CalculateSchedule(int? HH, int? MM, int? SS) => $"{HH ?? 0:D2}:{MM ?? 1:D2}:{SS ?? 0:D2}";

	internal static string CalculateScheduleStr(string value, string pattern1, string pattern2) {
		string sReturn = "00";
		try {
			string[] schedule = Regex.Split(value, pattern2);
			if (schedule.Length == 3) {
				return pattern1 switch {
					"HH" => schedule[0],
					"MM" => schedule[1],
					"SS" => schedule[2],
					_ => sReturn
				};
			}

			return sReturn;
		}
		catch (Exception) {
			return sReturn;
		}
	}

	internal static bool UpdateMeasureDataIsProcessed(ApplicationDbContext dbc, long measureDefId, int userId) {
		var lastUpdatedOn = DateTime.Now;
		try {
			_ = dbc.MeasureData
				.Where(md => md.Measure!.MeasureDefinition!.Id == measureDefId)
				.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, (byte)IsProcessed.measureData)
					.SetProperty(md => md.UserId, userId)
					.SetProperty(md => md.LastUpdatedOn, lastUpdatedOn));
			return true;
		}
		catch {
			return false;
		}
	}

	internal static bool UpdateMeasureDataIsProcessed(ApplicationDbContext dbc, long measureId, int userId, DateTime lastUpdatedOn, IsProcessed isProcessed) {
		try {
			_ = dbc.MeasureData
					.Where(md => md.Measure!.Id == measureId)
					.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, (byte)isProcessed)
						.SetProperty(md => md.UserId, userId)
						.SetProperty(md => md.LastUpdatedOn, lastUpdatedOn));
			return true;
		}
		catch {
			return false;
		}
	}

	internal static bool CreateMeasureDataRecords(ApplicationDbContext dbc, int intervalId, long? measureDefIf = null) {
		// Create measure dtaa records if don't exist
		int p_interval = intervalId, p_days_offset = 0;
		try {
			_ = dbc.Database.ExecuteSql($"spMeasureData {p_interval}, {p_days_offset}");
			return true;
		}
		catch {
			return false;
		}
	}

	public static double? RoundNullable(this double? value, int digits) {
		return value switch {
			double v => Math.Round(v, digits, MidpointRounding.AwayFromZero),
			null => null
		};
	}
}