namespace Deliver.WebApi.Data;

public class IntervalListObject
{
	public int CalendarId { get; set; }

	public List<GetIntervalsObject> data { get; } = new();
}
