namespace CLS.WebApi.Data;
using static CLS.WebApi.Helper;

public class UserObject
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public string? Department { get; set; }

	public Roles RoleId { get; set; }

	public string Role { get; set; } = null!;

	public DateTimeOffset? expiresUtc { get; set; }

	public DateTime LastModified { get; set; }

	public List<int> hierarchyIds = new ();

	public List<UserCalendarLocks> calendarLockIds = new();

	public Dictionary<pages, FilterSaveObject> savedFilters = new ()
	{
		{ pages.measureData, new FilterSaveObject() },
		{ pages.target, new FilterSaveObject() },
		{ pages.measure, new FilterSaveObject() },
		{ pages.measureDefinition, new FilterSaveObject() },
		{ pages.dataImports, new FilterSaveObject() }
	};
}
