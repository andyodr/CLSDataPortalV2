namespace Deliver.WebApi.Data;

public sealed class MeasureTypeRegionsObject
{
	public long Id { get; set; }

	public string? Name { get; set; }

	public string? Owner { get; set; }

	public List<RegionActiveCalculatedObject> Hierarchy { get; set; } = null!;
}
