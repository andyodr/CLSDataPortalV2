namespace CLS.WebApi.Data;

public class MeasureTypeDataObject
{
	public long measureDefinitionId { set; get; }
	public string measureName { set; get; }
	public string? measureTypeName { set; get; }
	public string? owner { set; get; }
	public List<RegionOwnerObject> hierarchy { get; set; }
}
