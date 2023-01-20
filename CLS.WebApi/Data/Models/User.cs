namespace CLS.WebApi.Data.Models;

public class User
{
	public int Id { set; get; }

	public string UserName { set; get; } = null!;

	public string LastName { set; get; } = null!;

	public string FirstName { set; get; } = null!;

	public string Department { get; set; } = null!;

	public UserRole UserRole { set; get; } = null!;

	public bool Active { set; get; } = true;

	public DateTime LastUpdatedOn { set; get; }
}
