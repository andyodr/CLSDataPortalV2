namespace Deliver.WebApi.Data;

public sealed class RegionMetricsFilterObject
{
	public ErrorModel Error { get; set; } = null!;

	public IList<RegionsDataViewModel> Data { get; set; } = null!;

	public IList<RegionFilterObject> Hierarchy { get; set; } = null!;

	public IList<HierarchyLevelDto> Levels { get; set; } = null!;

	public int RegionId { get; set; }
}
