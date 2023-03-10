namespace CLS.WebApi.Data.Models;

public class MeasureType
{
	public int Id { get; set; }

	public string Name { get; set; } = null!;

	public string? Description { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
