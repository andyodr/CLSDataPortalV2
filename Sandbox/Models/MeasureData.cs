namespace CLS.WebApi.Data.Models;

public class MeasureData
{
	public long Id { get; set; }

	public Measure? Measure { get; set; }

	public Calendar? Calendar { get; set; }

	public long TargetId { get; set; }

	public Target? Target { get; set; }

	public int UserId { get; set; }

	public User? User { get; set; }

	public double? Value { get; set; }

	public string Explanation { get; set; } = null!;

	public string Action { get; set; } = null!;

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
