namespace Deliver.WebApi.Data;

public class DataImportsMainObject
{
	public ErrorModel? Error { get; set; }

	public string CalculationTime { get; set; } = null!;

	public IList<IntervalsObject> Intervals { get; set; } = null!;

	public IList<YearsObject> Years { get; set; } = null!;

	public IList<DataImportObject> DataImport { get; set; } = null!;

	public int? IntervalId { get; set; }

	public int? CalendarId { get; set; }

	public int? CurrentYear { get; set; }
}
