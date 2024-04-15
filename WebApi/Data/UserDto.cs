namespace Deliver.WebApi.Data;

using System.Security.Claims;
using static Deliver.WebApi.Helper;

public sealed class UserDto
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

	public Dictionary<Pages, FilterSaveDto> savedFilters = new ()
	{
		{ Pages.MeasureData, new FilterSaveDto() },
		{ Pages.Target, new FilterSaveDto() },
		{ Pages.Measure, new FilterSaveDto() },
		{ Pages.MeasureDefinition, new FilterSaveDto() },
		{ Pages.DataImports, new FilterSaveDto() }
	};

	public static implicit operator UserDto(ClaimsPrincipal userClaim) {
		var claimUserId = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (claimUserId is string userId) {
			return new UserDto {
				Id = int.Parse(userId),
				UserName = userClaim.Identity!.Name!
			};
		}

		return new UserDto();
	}
}
