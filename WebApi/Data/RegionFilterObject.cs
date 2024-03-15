using System.Text.Json.Serialization;

namespace Deliver.WebApi.Data;

public sealed class RegionFilterObject
{
	public string Hierarchy { get; set; } = null!;

	public int Id { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Count { get; set; }

	public ICollection<RegionFilterObject> Sub { get; set; } = [];

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Found { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ErrorModel? Error { get; set; }
}
