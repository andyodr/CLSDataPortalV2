namespace CLS.WebApi.Data.Models;

public class Target
{
	public long Id { set; get; }

	public Measure? Measure { get; set; } = null!;

	public double? Value { set; get; }

	public double? YellowValue { set; get; }

	public bool Active { get; set; } = false;

	public User? User { get; set; } = null!;

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { set; get; }
}
