namespace CLS.WebApi.Data.Models;

public class MeasureDefinition
{
	public long Id { set; get; }

	public MeasureType MeasureType { set; get; } = null!;

	public int ReportIntervalId { set; get; }

	public string Name { set; get; } = string.Empty;

	public string VariableName { set; get; } = string.Empty;

	public string Description { set; get; } = null!;

	public string Expression { set; get; } = null!;

	public byte Precision { set; get; }

	public short Priority { set; get; }

	public short FieldNumber { set; get; }

	public Unit? Unit { set; get; } = null!;

	public bool? Calculated { set; get; } = null;

	public bool? AggDaily { set; get; } = null;

	public bool? AggWeekly { set; get; } = null;

	public bool? AggMonthly { set; get; } = null;

	public bool? AggQuarterly { set; get; } = null;

	public bool? AggYearly { set; get; } = null;

	public byte? AggFunction { get; set; } = null;

	public byte IsProcessed { get; set; }

	public DateTime? LastUpdatedOn { set; get; } = null;
}
