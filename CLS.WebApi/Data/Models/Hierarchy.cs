namespace CLS.WebApi.Data.Models;

public class Hierarchy
{
	/// <summary>
	/// The unique id and primary key for this Hierarchy
	/// </summary>
	public int Id { get; set; }

	public int HierarchyLevelId { get; set; }

	public HierarchyLevel? HierarchyLevel { get; set; }

	public int? HierarchyParentId { get; set; }

	public Hierarchy? Parent { get; set; }

	public List<Hierarchy>? Children { get; } = new();

	public string Name { get; set; } = null!;

	public bool? Active { get; set; }

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
