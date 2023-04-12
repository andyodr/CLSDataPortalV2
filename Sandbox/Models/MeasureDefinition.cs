namespace CLS.WebApi.Data.Models;

public class MeasureDefinition
{
	public long Id { get; set; }

	public MeasureType? MeasureType { get; set; }

	public Interval? ReportInterval { get; set; }

	public List<Measure>? Measures { get; } = new();

	public string Name { get; set; } = string.Empty;

	public string VariableName { get; set; } = string.Empty;

	public string? Description { get; set; } = null!;

	public string? Expression { get; set; }

	public byte Precision { get; set; }

	public short Priority { get; set; }

	public short FieldNumber { get; set; }

	public int UnitId { get; set; }

	public Unit? Unit { get; set; }

	public bool? Calculated { get; set; }

	public bool? AggDaily { get; set; }

	public bool? AggWeekly { get; set; }

	public bool? AggMonthly { get; set; }

	public bool? AggQuarterly { get; set; }

	public bool? AggYearly { get; set; }

	public byte? AggFunction { get; set; }

	public byte IsProcessed { get; set; }

	public DateTime? LastUpdatedOn { get; set; }
}
