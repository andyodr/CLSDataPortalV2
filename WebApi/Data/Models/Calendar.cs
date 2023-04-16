namespace Deliver.WebApi.Data.Models;

public class Calendar
{
	/// <summary>
	/// The unique id and primary key for this Calendar
	/// </summary>
	public int Id { get; set; }

	public int IntervalId { get; set; }

	public Interval Interval { get; set; } = null!;

	public byte? WeekNumber { get; set; }

	public byte? Month { get; set; }

	public byte? Quarter { get; set; }

	public short Year { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public bool Locked { get; set; }

	public byte IsProcessed { get; set; }

	public List<UserCalendarLock> UserCalendarLocks { get; set; } = null!;

	public DateTime LastUpdatedOn { get; set; }
}
