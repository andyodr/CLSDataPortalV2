namespace CLS.WebApi.Data;

public class SheetDataMeasureData
{
	public int? HierarchyID { get; set; }

	public long? MeasureID { get; set; }

	public double? Value { get; set; }

	public string? Action { get; set; }

	public string? Explanation { get; set; }

	public int RowNumber { get; set; }

	public int UnitId { get; set; }

	public byte Precision { get; set; }
}
