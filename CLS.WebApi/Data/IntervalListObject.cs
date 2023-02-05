namespace CLS.WebApi.Data;

public class IntervalListObject
{
	public int calendarId { get; set; }
	public List<GetIntervalsObject> data { get; } = new();
}
