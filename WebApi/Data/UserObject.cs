namespace Deliver.WebApi.Data;

using System.Security.Claims;
using static Deliver.WebApi.Helper;

public sealed class UserObject
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

	public Dictionary<Pages, FilterSaveObject> savedFilters = new ()
	{
		{ Pages.MeasureData, new FilterSaveObject() },
		{ Pages.Target, new FilterSaveObject() },
		{ Pages.Measure, new FilterSaveObject() },
		{ Pages.MeasureDefinition, new FilterSaveObject() },
		{ Pages.DataImports, new FilterSaveObject() }
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
