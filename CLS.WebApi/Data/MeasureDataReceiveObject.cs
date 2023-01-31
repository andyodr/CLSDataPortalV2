namespace CLS.WebApi.Data;

public class MeasureDataReceiveObject
{
	public int? calendarId { set; get; }
	public string day { set; get; }
	public int hierarchyId { set; get; }
	public int measureTypeId { set; get; }
	public long? measureDataId { set; get; }
	public double? measureValue { set; get; }
	public string explanation { set; get; }
	public string action { set; get; }
	//public ErrorModel error { set; get; }
}
