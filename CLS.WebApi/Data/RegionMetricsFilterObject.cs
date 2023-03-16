namespace CLS.WebApi.Data;

public class RegionMetricsFilterObject
{
	public ErrorModel Error { get; set; } = null!;

	public List<RegionsDataViewModel> Data { get; set; } = null!;

	public List<RegionFilterObject> Hierarchy { get; set; } = null!;

	public List<LevelObject> Levels { get; set; } = null!;

	public int RegionId { get; set; }
}
