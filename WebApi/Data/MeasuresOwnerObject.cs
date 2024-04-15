namespace Deliver.WebApi.Data;

public sealed class MeasuresOwnerObject
{
	public long MeasureDefinitionId { get; init; }

	public int MeasureTypeId { get; init; }

	public int HierarchyId { get; init; }

	public List<RegionOwnerObject>? Hierarchy { get; init; }

	public string Owner { get; init; } = null!;
}
