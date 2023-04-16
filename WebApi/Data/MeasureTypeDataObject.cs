namespace Deliver.WebApi.Data;

public class MeasureTypeDataObject
{
	public long MeasureDefinitionId { set; get; }

	public string MeasureName { set; get; } = null!;

	public string? MeasureTypeName { set; get; }

	public string? Owner { set; get; }

	public ICollection<RegionOwnerObject> Hierarchy { get; set; } = new List<RegionOwnerObject>();
}
