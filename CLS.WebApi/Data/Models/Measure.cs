namespace CLS.WebApi.Data.Models;

public class Measure
{
	public long Id { set; get; }

	public Hierarchy Hierarchy { set; get; } = null!;

	public MeasureDefinition? MeasureDefinition { set; get; } = null!;

	public bool? Active { set; get; } = null;

	public bool? Expression { set; get; } = null;

	public bool? Rollup { set; get; } = null;

	public string? Owner { set; get; } = null;

	public DateTime LastUpdatedOn { set; get; }
}
