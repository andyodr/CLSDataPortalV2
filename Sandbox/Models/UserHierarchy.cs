namespace CLS.WebApi.Data.Models;

public class UserHierarchy
{
	public int Id { set; get; }

	public User User { set; get; } = null!;

	public Hierarchy? Hierarchy { set; get; } = null!;

	public DateTime LastUpdatedOn { set; get; }
}
