namespace CLS.WebApi.Data;

public class RegionsDataViewModelAdd
{
	public int LevelId { get; set; }

	public string Name { get; set; } = null!;

	public int? ParentId { get; set; }

	public bool Active { get; set; }

	public bool Remove { get; set; }
}

public class RegionsDataViewModel: RegionsDataViewModelAdd
{
	public int Id { get; set; }

	public string? Level { get; set; }

	public string? ParentName { get; set; }
}
