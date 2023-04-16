namespace Deliver.WebApi.Data;

public class MeasureCalculatedObject
{
	public int reportIntervalId { get; set; }
	public bool calculated { get; set; }
	public bool aggDaily { get; set; }
	public bool aggWeekly { get; set; }
	public bool aggMonthly { get; set; }
	public bool aggQuarterly { get; set; }
	public bool aggYearly { get; set; }
}
