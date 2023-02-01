namespace CLS.WebApi.Data;

public class RegionIndexGetReturnObject
{
	public ErrorModel error { set; get; }
	public List<string> hierarchy { set; get; }
	public bool allow { set; get; }
	public List<MeasureTypeRegionsObject> data { set; get; }
}
