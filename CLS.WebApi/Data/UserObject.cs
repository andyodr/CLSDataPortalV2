namespace CLS.WebApi.Data;

public class UserObject
{
	public int userId { get; set; }

	public string userName { get; set; }

	public string? firstName { get; set; }

	public int userRoleId { get; set; }

	public string userRole { get; set; }

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
