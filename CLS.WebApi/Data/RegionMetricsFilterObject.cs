namespace CLS.WebApi.Data;

public class RegionMetricsFilterObject
{
	public ErrorModel error { set; get; }
	public List<RegionsDataViewModel> data { set; get; }
	public List<RegionFilterObject> hierarchy { set; get; }
	public List<LevelObject> levels { set; get; }
	public int regionId { set; get; }
}
