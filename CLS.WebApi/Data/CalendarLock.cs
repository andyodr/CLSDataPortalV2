namespace CLS.WebApi.Data;

public class CalendarLock
{
	public int id { set; get; }
	public string month { set; get; }
	public string startDate { set; get; }
	public string endDate { set; get; }
	public bool? locked { set; get; }
}
