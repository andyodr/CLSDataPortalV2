namespace CLS.WebApi.Data.Models;

public class UserCalendarLock
{
	public int Id { get; set; }

	public User User { get; set; } = null!;

	public int CalendarId { get; set; }

	public Calendar Calendar { get; set; } = null!;

	public bool? LockOverride { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
