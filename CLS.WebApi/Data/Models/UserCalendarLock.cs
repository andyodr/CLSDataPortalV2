namespace CLS.WebApi.Data.Models;

public class UserCalendarLock
{
	public int Id { set; get; }

	public User User { set; get; } = null!;

	public Calendar? Calendar { set; get; } = null!;

	public bool? LockOverride { set; get; }

	public DateTime LastUpdatedOn { set; get; }
}
