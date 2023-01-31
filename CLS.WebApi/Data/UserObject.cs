namespace CLS.WebApi.Data;

public class UserObject
{
	public int userId { set; get; }
	public string userName { set; get; }
	public string firstName { set; get; }
	public int userRoleId { set; get; }
	public string userRole { set; get; }
	public DateTimeOffset? expiresUtc { get; set; }

	public List<int> hierarchyIds = new ();

	public List<UserCalendarLocks> calendarLockIds = new();

	public Dictionary<Helper.pages, bool> Authorized = new ()
	{
		{ Helper.pages.measureData, false },
		{ Helper.pages.target, false } ,
		{ Helper.pages.measure, false },
		{ Helper.pages.hierarchy, false },
		{ Helper.pages.measureDefinition, false },
		{ Helper.pages.dataImports, false },
		{ Helper.pages.settings, false },
		{ Helper.pages.users, false }
	};

	public Dictionary<Helper.pages, FilterSaveObject> savedFilters = new ()
	{
		{ Helper.pages.measureData, new FilterSaveObject() },
		{ Helper.pages.target, new FilterSaveObject() },
		{ Helper.pages.measure, new FilterSaveObject() },
		{ Helper.pages.measureDefinition, new FilterSaveObject() },
		{ Helper.pages.dataImports, new FilterSaveObject() }
	};
}
