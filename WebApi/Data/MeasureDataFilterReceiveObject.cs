namespace CLS.WebApi.Data;

public class MeasureDataFilterReceiveObject
{
	public int? intervalId { set; get; }
	public int? year { set; get; }
	public bool? isDataImport { get; set; }
}
