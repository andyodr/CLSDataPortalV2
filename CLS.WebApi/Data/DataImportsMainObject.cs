namespace CLS.WebApi.Data;

public class DataImportsMainObject
{
	public ErrorModel error { set; get; }
	//public CalculationTimeObject calculationTime { set; get; }
	public string calculationTime { get; set; }
	public List<IntervalsObject> intervals { set; get; }
	public List<YearsObject> years { set; get; }
	public List<DataImportObject> dataImport { set; get; }
	public int? intervalId { get; set; }
	public int? calendarId { get; set; }
	public int? currentYear { get; set; }
}
