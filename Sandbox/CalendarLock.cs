namespace CLS.WebApi.Data;

public class CalendarLock
{
	public int Id { set; get; }
	public string? Month { set; get; }
	public string? StartDate { set; get; }
	public string? EndDate { set; get; }
	public bool? Locked { set; get; }
}
