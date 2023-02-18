namespace CLS.WebApi.Data;

public class MeasuresOwnerObject
{
	public long MeasureDefinitionId { get; set; }

	public int MeasureTypeId { get; set; }

	public int HierarchyId { get; set; }

	public List<RegionOwnerObject>? Hierarchy { get; set; }

	public string Owner { get; set; } = null!;
}
