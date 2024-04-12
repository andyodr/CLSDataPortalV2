using Deliver.WebApi.Data;
using Deliver.WebApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using static Deliver.WebApi.Helper;

namespace Deliver.WebApi.Extensions;

public static class Extensions
{
	public static IList<RegionsDataViewModel> OrderByHierarchy(this IEnumerable<RegionsDataViewModel> regions, RegionsDataViewModel parent = null!) {
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
		IQueryable<Calendar>? cal;
		switch ((Intervals)calendar.IntervalId) {
			case Intervals.Monthly:
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

				break;
			case Intervals.Weekly:
				// This is a fix because Settings page does not have calendarLock by other intervals yet. Only monthly.
				cal = dbc.Calendar
					.Where(c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year
						&& c.StartDate >= calendar.StartDate && c.EndDate <= calendar.StartDate)
					.Include(c => c.UserCalendarLocks.Where(l => l.UserId == userId));
				foreach (var c in cal) {
					if (c.Locked == true) {
						isLocked = true;
						foreach (var userLock in c.UserCalendarLocks) {
							if (userLock.LockOverride ?? false) {
								isLockedOverride = true;
								break;
							}
						}
					}
				}

				break;
			case Intervals.Quarterly:
				cal = dbc.Calendar
					.Where(c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year && c.Quarter == calendar.Quarter)
					.Include(c => c.UserCalendarLocks.Where(l => l.UserId == userId));
				foreach (var c in cal) {
					if (c.Locked == true) {
						isLocked = true;
						foreach (var itemUserCal in c.UserCalendarLocks) {
							if (itemUserCal.LockOverride ?? false) {
								isLockedOverride = true;
								break;
							}
						}
					}
				}

				break;
			case Intervals.Yearly:
				cal = dbc.Calendar
					.Where(c => c.IntervalId == (int)Intervals.Monthly && c.Year == calendar.Year)
					.Include(c => c.UserCalendarLocks.Where(l => l.UserId == userId));
				foreach (var c in cal) {
					if (c.Locked == true) {
						isLocked = true;
						foreach (var itemUserCal in c.UserCalendarLocks) {
							if (itemUserCal.LockOverride ?? false) {
								isLockedOverride = true;
								break;
							}
						}
					}
				}

				break;
		}

		return isLocked && !isLockedOverride;
	}

	public static bool AddAuditTrail(this ApplicationDbContext dbc, string type, string code, string description, string data, DateTime lastUpdatedOn, int? userId = null) {
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

	internal static ErrorModel ErrorProcessing(this ApplicationDbContext db, Exception e, int? userId) {
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

	public static ImportErrorResult? ValidateTargetImport(this ApplicationDbContext dbc, ImportTarget row, int userId) {
		//check for null values
		if (row.MeasureDefinitionId is null) {
			return new() { Row = row.RowNumber, Message = Resource.DI_ERR_MEASURE_NULL };
		}

		if (row.HierarchyId is null) {
			return new() { Row = row.RowNumber, Message = Resource.DI_ERR_HIERARCHY_NULL };
		}

		//check userHierarchy
		if (dbc.IsHierarchyValidated(row.RowNumber, (int)row.HierarchyId, null, userId) is ImportErrorResult err) {
			return err;
		};

		var units = dbc.Measure.Where(m => m.Active == true
				&& m.MeasureDefinitionId == row.MeasureDefinitionId
				&& m.HierarchyId == row.HierarchyId)
			.Select(m => m.MeasureDefinition!.UnitId)
			.Distinct()
			.ToArray();
		if (units.Length != 0) {
			foreach (var unit in units) {
				if (row.Target != null && unit == 1 && (row.Target < 0 || row.Target > 1)) {
					return new() { Row = row.RowNumber, Message = Resource.VAL_TARGET_UNIT };
				}

				if (row.Yellow != null && unit == 1 && (row.Yellow < 0 || row.Yellow > 1)) {
					return new() { Row = row.RowNumber, Message = Resource.VAL_YELLOW_UNIT };
				}
			}
		}
		else {
			return new() { Row = row.RowNumber, Message = Resource.DI_ERR_NO_MEASURE };
		}

		return null;
	}

	public static ImportErrorResult? IsHierarchyValidated(this ApplicationDbContext dbc, int rowNumber, int hierarchyId, double? value, int userId) {
		var hierarchy = dbc.Hierarchy.Where(h => h.Id == hierarchyId).Select(h => h.Active ?? false).ToArray();
		if (hierarchy.Length == 0) {
			return new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_EXIST };
		}
		else if (!hierarchy.Any(x => x)) {
			return new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_ACTIVE };
		}
		else if (!dbc.UserHierarchy.Where(u => u.UserId == userId && u.HierarchyId == hierarchyId).Any()) {
			return new() { Row = rowNumber, Message = Resource.DI_ERR_HIERARCHY_NO_ACCESS };
		}

		return default;
	}

	public static ImportErrorResult? ImportTarget(this ApplicationDbContext dbc, ImportTarget target, int userId, DateTime? time = null) {
		var now = time ?? DateTime.Now;
		try {
			var measure = dbc.Measure.Where(m => m.HierarchyId == target.HierarchyId
					&& m.MeasureDefinitionId == target.MeasureDefinitionId)
				.Include(m => m.Targets)
				.Include(m => m.MeasureData.Where(md => now >= md.Calendar!.StartDate && now <= md.Calendar.EndDate))
				.Select(m => new { m.Id, m.Targets, m.MeasureData })
				.First();
			double? targetValue = target.Target.RoundNullable(target.Precision);
			double? yellowValue = target.Yellow.RoundNullable(target.Precision);

			var existingTarget = measure.Targets?.FirstOrDefault(t => t.Value == targetValue && t.YellowValue == yellowValue);
			if (existingTarget is null) {
				var dupTarget = measure.Targets?.GroupBy(t => new { t.Value, t.YellowValue })
					.Where(g => g.Count() > 1)
					.Select(g => g.First())
					.FirstOrDefault();
				if (dupTarget is not null) {
					dupTarget.Value = targetValue;
					dupTarget.YellowValue = yellowValue;
					dupTarget.LastUpdatedOn = now;
					dupTarget.Active = true;
					dupTarget.UserId = userId;
					dupTarget.IsProcessed = (byte)IsProcessed.Complete;
					foreach (var md in measure.MeasureData) {
						md.Target = dupTarget;
						md.LastUpdatedOn = now;
						md.IsProcessed = (byte)IsProcessed.Complete;
					}

					foreach (var t in measure.Targets!.Where(t => t.Active)) {
						if (t != dupTarget) {
							t.Active = false;
							t.LastUpdatedOn = now;
							t.IsProcessed = (byte)IsProcessed.Complete;
						}
					}
				}
				else {
					foreach (var t in measure.Targets?.Where(t => t.Active) ?? []) {
						t.Active = false;
						t.LastUpdatedOn = now;
						t.IsProcessed = (byte)IsProcessed.Complete;
					}

					Target mdTarget = new() {
						MeasureId = measure.Id,
						Value = target.Target,
						YellowValue = yellowValue,
						Active = true,
						UserId = userId,
						IsProcessed = (byte)IsProcessed.Complete
					};
					foreach (var md in measure.MeasureData) {
						md.Target = mdTarget;
						md.LastUpdatedOn = now;
						md.IsProcessed = (byte)IsProcessed.Complete;
					}
				}
			}
			else {
				if (!existingTarget.Active) {
					existingTarget.Active = true;
					existingTarget.LastUpdatedOn = now;
					existingTarget.IsProcessed = (byte)IsProcessed.Complete;
				}

				foreach (var md in measure.MeasureData.Where(md => md.Target != existingTarget)) {
					md.Target = existingTarget;
					md.LastUpdatedOn = now;
					md.IsProcessed = (byte)IsProcessed.Complete;
				}

				foreach (var t in measure.Targets!.Where(t => t != existingTarget && t.Active)) {
					t.Active = false;
					t.LastUpdatedOn = now;
					t.IsProcessed = (byte)IsProcessed.Complete;
				}
			}

			_ = dbc.SaveChanges();
		}
		catch {
			return new() { Row = target.RowNumber, Message = Resource.DI_ERR_UPLOADING };
		}

		return null;
	}
}
