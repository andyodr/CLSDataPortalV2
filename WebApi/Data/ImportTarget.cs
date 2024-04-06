namespace Deliver.WebApi.Data;

public sealed class ImportTarget
{
	public int? HierarchyId { get; set; }

	public long? MeasureDefinitionId { get; set; }

	public double? Target { get; set; }

	public double? Yellow { get; set; }

	public int RowNumber { get; set; }

	public int UnitId { get; set; }

	public byte Precision { get; set; }
}
