namespace Deliver.WebApi.Data;

public class RegionsDataViewModelAdd
{
	public int LevelId { get; init; }

	public string Name { get; init; } = null!;

	public int? ParentId { get; init; }

	public bool Active { get; init; }

	public bool Remove { get; set; }
}

public sealed class RegionsDataViewModel : RegionsDataViewModelAdd
{
	public int Id { get; init; }

	public string? Level { get; init; }

	public string? ParentName { get; init; }
}
