namespace Deliver.WebApi.Data.Models;

public sealed class UserHierarchy
{
	public int Id { get; set; }

	public int UserId { get; set; }

	public User User { get; set; } = null!;

	public int HierarchyId { get; set; }

	public Hierarchy Hierarchy { get; set; } = null!;

	public DateTime LastUpdatedOn { get; set; }
}
