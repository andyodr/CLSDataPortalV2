namespace Deliver.WebApi.Data.Models;

public sealed class MeasureType
{
	public int Id { get; set; }

	public string Name { get; set; } = null!;

	public string? Description { get; set; }

	public DateTime LastUpdatedOn { get; set; }

	public IReadOnlyList<MeasureDefinition>? MeasureDefinitions { get; set; }
}
