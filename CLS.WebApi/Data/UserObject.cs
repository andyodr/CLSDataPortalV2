namespace CLS.WebApi.Data;

public class UserObject
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public string? Department { get; set; }

	public int RoleId { get; set; }

	public string Role { get; set; } = null!;

	public DateTimeOffset? expiresUtc { get; set; }

	public DateTime LastModified { get; set; }

	public List<int> hierarchyIds = new ();

	public List<UserCalendarLocks> calendarLockIds = new();

	public Dictionary<Helper.pages, FilterSaveObject> savedFilters = new ()
	{
		{ Helper.pages.measureData, new FilterSaveObject() },
		{ Helper.pages.target, new FilterSaveObject() },
		{ Helper.pages.measure, new FilterSaveObject() },
		{ Helper.pages.measureDefinition, new FilterSaveObject() },
		{ Helper.pages.dataImports, new FilterSaveObject() }
	};
}
