namespace Deliver.WebApi.Data;

public sealed class IntervalListObject
{
	public int CalendarId { get; set; }

	public List<GetIntervalsObject> data { get; } = new();
}
