namespace CLS.WebApi.Data.Models;

public class Hierarchy
{
	/// <summary>
	/// The unique id and primary key for this Hierarchy
	/// </summary>
	public int Id { get; set; }

	public HierarchyLevel HierarchyLevel { set; get; } = null!;

	public int? HierarchyParentId { set; get; } = null;

	public string Name { get; set; } = null!;

	public bool? Active { get; set; }

	public byte IsProcessed { get; set; }

	public DateTime LastUpdatedOn { set; get; }
}
