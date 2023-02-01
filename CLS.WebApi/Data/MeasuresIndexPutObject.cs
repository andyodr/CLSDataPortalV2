namespace CLS.WebApi.Data;

public class MeasuresIndexPutObject
{
	public ErrorModel error { set; get; }
	public long measureDefinitionId { get; set; }
	public List<RegionActiveCalculatedObject> hierarchy { set; get; }
}
