using System.Text.Json.Serialization;

namespace Deliver.WebApi.Data;

public sealed class RegionIndexGetResponse
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ErrorModel? Error { get; set; }

	public IReadOnlyList<string> Hierarchy { get; set; } = [];

	public bool Allow { get; set; }

	public IList<MeasureTypeRegionsObject> Data { get; set; } = [];
}
