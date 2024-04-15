namespace Deliver.WebApi.Data;

public sealed class SheetDataMeasureData
{
	public int? HierarchyId { get; init; }

	public long? MeasureDefinitionId { get; init; }

	public double? Value { get; init; }

	public string? Action { get; init; }

	public string? Explanation { get; init; }

	public int RowNumber { get; init; }

	public int UnitId { get; set; }

	public byte Precision { get; set; }
}
