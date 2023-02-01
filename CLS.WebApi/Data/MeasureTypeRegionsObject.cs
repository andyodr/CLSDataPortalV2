namespace CLS.WebApi.Data;

public class MeasureTypeRegionsObject
{
	public long id { set; get; }
	public string? name { set; get; }
	public string? owner { set; get; }
	public List<RegionActiveCalculatedObject> hierarchy { set; get; }
}
