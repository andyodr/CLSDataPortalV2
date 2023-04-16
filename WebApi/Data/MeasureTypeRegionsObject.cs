namespace Deliver.WebApi.Data;

public class MeasureTypeRegionsObject
{
	public long Id { get; set; }

	public string? Name { get; set; }

	public string? Owner { get; set; }

	public List<RegionActiveCalculatedObject> Hierarchy { get; set; } = null!;
}
