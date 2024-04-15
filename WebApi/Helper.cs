using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Deliver.WebApi.Data.Models;
using Deliver.WebApi.Data;

namespace Deliver.WebApi;

public static class Helper
{
	public enum Pages { MeasureData, Target, Measure, MeasureDefinition, Hierarchy, DataImports, Settings, Users }

	public enum Intervals { Daily = 1, Weekly = 2, Monthly = 3, Quarterly = 4, Yearly = 5 };

	public enum Roles { PowerUser = 1, RegionalAdministrator = 2, SystemAdministrator = 3 }

	public enum DataImports { MeasureData = 1, Target = 2, Customer = 3 }

	public enum IsProcessed { No, MeasureData, Complete }

	internal static int FindPreviousCalendarId(DbSet<Calendar> calendarRepo, int intervalId) {
		return calendarRepo.Where(c => c.Interval.Id == intervalId && c.EndDate <= DateTime.Today).OrderByDescending(d => d.EndDate).First().Id;
	}

	internal static DataImportsResponseDataImportElement DataImportHeading(DataImports dataImport) {
		DataImportsResponseDataImportElement result = new() { Heading = [] };

		if (dataImport == DataImports.Target) {
			result.Id = (int)DataImports.Target;
			result.Name = "Target";
			result.Heading.Add(new() { Title = "hierarchyid", Required = true });
			result.Heading.Add(new() { Title = "measureid", Required = true });
			result.Heading.Add(new() { Title = "target", Required = true });
			result.Heading.Add(new() { Title = "yellow", Required = false });
		}
		else if (dataImport == DataImports.Customer) {
			result.Id = (int)DataImports.Customer;
			result.Name = "Customer Region";
			result.Heading.Add(new() { Title = "hierarchyid", Required = true });
			result.Heading.Add(new() { Title = "calendarid", Required = true });
			result.Heading.Add(new() { Title = "customergroup", Required = false });
			result.Heading.Add(new() { Title = "customersubgroup", Required = false });
			result.Heading.Add(new() { Title = "purchasetype", Required = false });
			result.Heading.Add(new() { Title = "tradechannel", Required = false });
			result.Heading.Add(new() { Title = "tradechannelgroup", Required = false });
			result.Heading.Add(new() { Title = "sales", Required = false });
			result.Heading.Add(new() { Title = "numorders", Required = false });
			result.Heading.Add(new() { Title = "numlines", Required = false });
			result.Heading.Add(new() { Title = "ordertype", Required = false });
			result.Heading.Add(new() { Title = "numlateorders", Required = false });
			result.Heading.Add(new() { Title = "numlatelines", Required = false });
			result.Heading.Add(new() { Title = "numordlens", Required = false });
			result.Heading.Add(new() { Title = "ordqty", Required = false });
			result.Heading.Add(new() { Title = "headerstatuscode", Required = false });
			result.Heading.Add(new() { Title = "headerstatus", Required = false });
			result.Heading.Add(new() { Title = "blockcode", Required = false });
			result.Heading.Add(new() { Title = "blocktext", Required = false });
			result.Heading.Add(new() { Title = "rejectioncode", Required = false });
			result.Heading.Add(new() { Title = "rejectiontext", Required = false });
			result.Heading.Add(new() { Title = "creditstatuscheck", Required = false });
			result.Heading.Add(new() { Title = "creditcode", Required = false });
		}
		else {
			result.Id = (int)DataImports.MeasureData;
			result.Name = "Measure Data";
			result.Heading.Add(new() { Title = "hierarchyid", Required = true });
			result.Heading.Add(new() { Title = "measureid", Required = true });
			result.Heading.Add(new() { Title = "value", Required = true });
			result.Heading.Add(new() { Title = "explanation", Required = false });
			result.Heading.Add(new() { Title = "action", Required = false });
		}

		return result;
	}

	public readonly struct DateTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
    {
		private readonly int years = years;
		private readonly int months = months;
		private readonly int days = days;
		private readonly int hours = hours;
		private readonly int minutes = minutes;
		private readonly int seconds = seconds;
		private readonly int milliseconds = milliseconds;

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
				(date2, date1) = (date1, date2);
			}

			DateTime current = date1;
			int years = 0;
			int months = 0;
			int days = 0;

			Phase phase = Phase.Years;
			DateTimeSpan span = new();
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
		UpdatedObject update = new() {
			By = userName,
			LongDt = lastUpdatedOn.ToString()
		};

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

	public static UserDto? CreateUserObject(ClaimsPrincipal userClaim) {
		UserDto user = userClaim;
		return user.Id > 0 ? user : null;
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
}
