namespace Deliver.WebApi.Data;

public sealed class UserCalendarLocks
{
	public int CalendarId { get; set; }

	public bool? LockOverride { get; set; }
}
