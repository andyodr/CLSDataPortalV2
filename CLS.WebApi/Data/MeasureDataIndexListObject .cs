namespace CLS.WebApi.Data;

public class MeasureDataIndexListObject
{
	public string range { set; get; }
	public int? calendarId { set; get; }
	public bool allow { set; get; }
	public bool editValue { set; get; }
	public bool locked { set; get; }
	public bool confirmed { get; set; }
	public FilterSaveObject filter { set; get; }
	public List<MeasureDataReturnObject> data { set; get; }
	public ErrorModel error { set; get; }
}
