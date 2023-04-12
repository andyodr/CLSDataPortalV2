namespace CLS.WebApi.Data.Models;

public class HierarchyLevel
{
	/// <summary>
	/// The unique id and primary key for this HierarchyLevel
	/// </summary>
	public int Id { set; get; }

	public string Name { set; get; } = null!;

	public short Level { set; get; }
}
