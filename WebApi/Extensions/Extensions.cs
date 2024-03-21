using Deliver.WebApi.Data;
using Deliver.WebApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Extensions;

public static class Extensions
{
	public static IList<RegionsDataViewModel> OrderByHierarchy(this IEnumerable<RegionsDataViewModel> regions, RegionsDataViewModel parent = null!)
	{
		List<RegionsDataViewModel> result = [];
		if (parent is null) {
			parent = regions.First(h => h.ParentId is null);
			result = [parent];
		}

		var children = regions.Where(h => h.ParentId == parent?.Id).OrderByDescending(h => h.Active).ThenBy(h => h.Id);
		foreach (var node in children) {
			result.Add(node);
			result.AddRange(regions.OrderByHierarchy(node));
		}

		// Add anything left in regions that is not in result
		if (result.Count > 0 && ReferenceEquals(parent, result[0])) {
			result.AddRange(regions.ExceptBy(result.Select(region => region.Id), region => region.Id));
		}

		return result;
	}

	public static double? RoundNullable(this double? value, int digits) {
		return value switch {
			double v => Math.Round(v, digits, MidpointRounding.AwayFromZero),
			null => null
		};
	}

	internal static bool IsMeasureCalculated(this ApplicationDbContext dbc, bool isCalculatedExpression, int hId, int intervalId, long measureDefId, MeasureCalculatedObject? measureCalculated = null) {
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
			measureCalculated = new() {
				ReportIntervalId = measureDef?.ReportIntervalId ?? 0,
				Calculated = measureDef?.Calculated ?? false,
				AggDaily = measureDef?.AggDaily ?? false,
				AggWeekly = measureDef?.AggWeekly ?? false,
				AggMonthly = measureDef?.AggMonthly ?? false,
				AggQuarterly = measureDef?.AggQuarterly ?? false,
				AggYearly = measureDef?.AggYearly ?? false
			};
		}

		// If Measure.Expression = 0, then check MeasureDefinition
		if (measureCalculated.Calculated) {
			return isCalculatedExpression; // This is false
		}
		else {
			if (measureCalculated.ReportIntervalId == intervalId) {
				return false;
			}

			// Checks aggregations from MeasureDefinition
			return (Intervals)intervalId switch {
				Intervals.Daily => measureCalculated.AggDaily,
				Intervals.Weekly => measureCalculated.AggWeekly,
				Intervals.Monthly => measureCalculated.AggMonthly,
				Intervals.Quarterly => measureCalculated.AggQuarterly,
				Intervals.Yearly => measureCalculated.AggYearly,
				_ => false
			};
		}
	}

	internal static bool IsDataLocked(this ApplicationDbContext dbc, int calendarId, int userId, Calendar calendar) {
		// --------------------------------- Lock Override ----------------------------
		bool isLocked = false;
		bool isLockedOverride = false;

		if (calendar.IntervalId == (int)Intervals.Monthly) {
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
		if (calendar.IntervalId == (int)Intervals.Weekly) {
			var cal = dbc.Calendar.Where(
			  c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year && c.StartDate >= calendar.StartDate && c.EndDate <= calendar.StartDate);
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
		if (calendar.IntervalId == (int)Intervals.Quarterly) {
			var cal = dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year && c.Quarter == calendar.Quarter);
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
		if (calendar.IntervalId == (int)Intervals.Yearly) {
			var cal = dbc.Calendar.Where(c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year);
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

	internal static bool UpdateMeasureDataIsProcessed(this ApplicationDbContext dbc, long measureId, int userId, DateTime lastUpdatedOn, IsProcessed isProcessed) {
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

	internal static bool UpdateMeasureDataIsProcessed(this ApplicationDbContext dbc, long measureDefId, int userId) {
		var lastUpdatedOn = DateTime.Now;
		try {
			_ = dbc.MeasureData
				.Where(md => md.Measure!.MeasureDefinition!.Id == measureDefId)
				.ExecuteUpdate(s => s.SetProperty(md => md.IsProcessed, (byte)IsProcessed.MeasureData)
					.SetProperty(md => md.UserId, userId)
					.SetProperty(md => md.LastUpdatedOn, lastUpdatedOn));
			return true;
		}
		catch {
			return false;
		}
	}
}
