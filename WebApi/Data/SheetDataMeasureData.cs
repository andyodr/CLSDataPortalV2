namespace Deliver.WebApi.Data;

public sealed class SheetDataMeasureData
{
	public int? HierarchyId { get; set; }

	public long? MeasureDefinitionId { get; set; }

	public double? Value { get; set; }

	public string? Action { get; set; }

	public string? Explanation { get; set; }

	public int RowNumber { get; set; }

	public int UnitId { get; set; }

	public byte Precision { get; set; }
}
