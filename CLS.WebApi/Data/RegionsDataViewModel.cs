namespace CLS.WebApi.Data;

public class RegionsDataViewModel
{
	public int Id { get; set; }

	public int? LevelId { get; set; }

	public string Level { get; set; } = null!;

	public string Name { get; set; } = null!;

	public int? ParentId { get; set; }

	public string ParentName { get; set; } = null!;

	public bool? Active { get; set; }

	public bool? Remove { get; set; }
}
