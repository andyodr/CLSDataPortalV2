namespace Deliver.WebApi.Data;

public sealed class RegionActiveCalculatedObject
{
	public long Id { get; set; }

	public bool Active { get; set; }

	public bool Expression { get; set; }

	public bool Rollup { get; set; }
}
