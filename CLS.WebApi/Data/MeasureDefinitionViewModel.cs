namespace CLS.WebApi.Data;

public class MeasureDefinitionViewModel
{
	public long? Id { get; set; }

	public string Name { get; set; } = null!;

	public int MeasureTypeId { get; set; }

	public string? Interval { get; set; }

	public int IntervalId { get; set; }

	public string VarName { get; set; } = null!;

	public string? Description { get; set; }

	public string? Expression { get; set; }

	public byte Precision { get; set; }

	public int Priority { get; set; }

	public short FieldNumber { get; set; }

	public int UnitId { get; set; }

	public string? Units { get; set; }

	public bool? Calculated { get; set; }

	public bool? Daily { get; set; }

	public bool? Weekly { get; set; }

	public bool? Monthly { get; set; }

	public bool? Quarterly { get; set; }

	public bool? Yearly { get; set; }

	public string? AggFunction { get; set; }

	public byte? AggFunctionId { get; set; }
}
