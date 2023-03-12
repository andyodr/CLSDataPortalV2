namespace CLS.WebApi.Data;

public class UserIndexDto
{
	public int? id { get; set; }

	public string userName { get; set; } = null!;

	public string? lastName { get; set; }

	public string? firstName { get; set; }

	public string? department { get; set; }

	public string? roleName { get; set; } = null!;

	public int roleId { get; set; }

	public List<int> hierarchiesId { get; set; } = null!;

	public string? hierarchyName { get; set; }

	public string? active { get; set; }
}
