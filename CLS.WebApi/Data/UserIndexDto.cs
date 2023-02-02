namespace CLS.WebApi.Data;

public class UserIndexDto
{
	public int? id { set; get; }

	public string userName { set; get; }

	public string lastName { set; get; }

	public string firstName { set; get; }

	public string department { set; get; }

	public string roleName { set; get; }

	public int roleId { set; get; }

	public List<int> hierarchiesId { set; get; }

	public string hierarchyName { set; get; }

	public string active { set; get; }
}
