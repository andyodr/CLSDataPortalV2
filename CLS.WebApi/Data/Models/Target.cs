namespace CLS.WebApi.Data.Models;

public class Target
{
	public long Id { get; set; }

	public long MeasureId { get; set;}

	public Measure? Measure { get; set; }

	public ICollection<MeasureData>? MeasureData { get; } = new HashSet<MeasureData>();

	public double? Value { get; set;}

	public double? YellowValue { get; set;}

	public bool Active { get; set; }

	public int? UserId { get; set; }

	public User? User { get; set; }

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { get; set;}
}
