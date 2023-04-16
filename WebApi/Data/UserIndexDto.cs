namespace Deliver.WebApi.Data;

public class UserIndexDto
{
	public int? Id { get; set; }

	public string UserName { get; set; } = null!;

	public string? LastName { get; set; }

	public string? FirstName { get; set; }

	public string? Department { get; set; }

	public string? RoleName { get; set; } = null!;

	public int RoleId { get; set; }

	public List<int> HierarchiesId { get; set; } = null!;

	public string? HierarchyName { get; set; }

	public bool? Active { get; set; }
}
