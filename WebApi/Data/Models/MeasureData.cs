namespace Deliver.WebApi.Data.Models;

public sealed class MeasureData
{
	public long Id { get; set; }

	public Measure? Measure { get; set; }

	public int CalendarId { get; set; }

	public Calendar? Calendar { get; set; }

	public long TargetId { get; set; }

	public Target? Target { get; set; }

	public int? UserId { get; set; }

	public User? User { get; set; }

	public double? Value { get; set; }

	public string? Explanation { get; set; }

	public string? Action { get; set; }

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
