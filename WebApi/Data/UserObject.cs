namespace Deliver.WebApi.Data;

using System.Security.Claims;
using static Deliver.WebApi.Helper;

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

	public static implicit operator UserObject(ClaimsPrincipal userClaim) {
		var claimUserId = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (claimUserId is string userId) {
			return new UserObject {
				Id = int.Parse(userId),
				UserName = userClaim.Identity!.Name!
			};
		}

		return new UserObject();
	}
}
