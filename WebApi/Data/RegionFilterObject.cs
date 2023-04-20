namespace Deliver.WebApi.Data;

public sealed class RegionFilterObject
{
	public string Hierarchy { get; set; } = null!;

	public int Id { get; set; }

	public int? Count { get; set; }

	public ICollection<RegionFilterObject> Sub { get; set; } = new List<RegionFilterObject>();

	public bool? Found { get; set; }

	public ErrorModel? Error { get; set; }
}
