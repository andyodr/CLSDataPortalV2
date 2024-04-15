namespace Deliver.WebApi.Data;

public sealed class ImportTarget
{
	public int? HierarchyId { get; init; }

	public long? MeasureDefinitionId { get; init; }

	public double? Target { get; init; }

	public double? Yellow { get; init; }

	public int RowNumber { get; init; }

	public int UnitId { get; init; }

	public byte Precision { get; set; }
}
