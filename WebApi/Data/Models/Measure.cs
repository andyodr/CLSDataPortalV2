namespace Deliver.WebApi.Data.Models;

public sealed class Measure
{
	public long Id { get; set; }

	public int HierarchyId { get; set; }

	public Hierarchy? Hierarchy { get; set; }

	public long MeasureDefinitionId { get; set; }

	public MeasureDefinition? MeasureDefinition { get; set; }

	public List<Target>? Targets { get; } = [];

	public ICollection<MeasureData> MeasureData { get; } = new HashSet<MeasureData>();

	public bool? Active { get; set; }

	public bool? Expression { get; set; }

	public bool? Rollup { get; set; }

	public string? Owner { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
