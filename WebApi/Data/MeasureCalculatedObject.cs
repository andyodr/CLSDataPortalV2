namespace Deliver.WebApi.Data;

public sealed class MeasureCalculatedObject
{
	public int ReportIntervalId { get; set; }

	public bool Calculated { get; set; }

	public bool AggDaily { get; set; }

	public bool AggWeekly { get; set; }

	public bool AggMonthly { get; set; }

	public bool AggQuarterly { get; set; }

	public bool AggYearly { get; set; }
}
