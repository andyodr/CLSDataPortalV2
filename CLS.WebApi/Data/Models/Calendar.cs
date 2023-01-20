namespace CLS.WebApi.Data.Models;

public class Calendar
{
	/// <summary>
	/// The unique id and primary key for this Calendar
	/// </summary>
	public int Id { set; get; }

	public Interval Interval { set; get; } = null!;

	public byte? WeekNumber { set; get; }

	public byte? Month { set; get; }

	public byte? Quarter { set; get; }

	public short Year { set; get; }

	public DateTime? StartDate { set; get; }

	public DateTime? EndDate { set; get; }

	public bool Locked { set; get; }

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { set; get; }
}
