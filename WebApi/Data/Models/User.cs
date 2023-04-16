namespace Deliver.WebApi.Data.Models;

public class User
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public string? LastName { get; set; }

	public string? FirstName { get; set; }

	public string? Department { get; set; }

	public UserRole UserRole { get; set; } = null!;

	public bool? Active { get; set; } = true;

	public List<UserHierarchy>? UserHierarchies { get; } = new();

	public List<UserCalendarLock>? UserCalendarLocks { get; } = new();

	public DateTime LastUpdatedOn { get; set; }
}
