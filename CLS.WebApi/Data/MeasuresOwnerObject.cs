namespace CLS.WebApi.Data;

public class MeasuresOwnerObject
{
	public long measureDefinitionId { get; set; }
	public int measureTypeId { get; set; }
	public int hierarchyId { get; set; }
	public List<RegionOwnerObject> hierarchy { get; set; }
	public string owner { get; set; }
}
