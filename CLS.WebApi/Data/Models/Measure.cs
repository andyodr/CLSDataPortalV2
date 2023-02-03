namespace CLS.WebApi.Data.Models;

public class Measure
{
	public long Id { get; set; }

	public Hierarchy? Hierarchy { get; set; }

	public int MeasureDefinitionId { get; set; }
	public MeasureDefinition? MeasureDefinition { get; set; }

	public List<Target>? Targets { get; set; }

	public bool? Active { get; set; }

	public bool? Expression { get; set; }

	public bool? Rollup { get; set; }

	public string? Owner { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
