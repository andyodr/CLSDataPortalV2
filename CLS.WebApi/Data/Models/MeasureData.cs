namespace CLS.WebApi.Data.Models;

public class MeasureData
{
	public long Id { set; get; }

	public Measure? Measure { set; get; } = null!;

	public Calendar? Calendar { set; get; } = null!;

	public Target? Target { get; set; } = null!;

	public User? User { set; get; } = null!;

	public double? Value { set; get; } = null;

	public string Explanation { set; get; } = null!;

	public string Action { set; get; } = null!;

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { set; get; }
}
