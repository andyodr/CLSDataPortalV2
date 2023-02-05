namespace CLS.WebApi.Data.Models;

public class User
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public string LastName { get; set; } = null!;

	public string FirstName { get; set; } = null!;

	public string Department { get; set; } = null!;

	public UserRole? UserRole { get; set; }

	public bool? Active { get; set; } = true;

	public DateTime LastUpdatedOn { get; set; }

	public List<UserHierarchy>? UserHierarchies { get; } = new();

	public List<UserCalendarLock>? UserCalendarLocks { get; } = new();
}
