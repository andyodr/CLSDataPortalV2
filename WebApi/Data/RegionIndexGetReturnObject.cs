using System.Text.Json.Serialization;

namespace Deliver.WebApi.Data;

public sealed class RegionIndexGetReturnObject
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ErrorModel? Error { get; set; }

	public IList<string> Hierarchy { get; set; } = new List<string>();

	public bool Allow { get; set; }

	public IList<MeasureTypeRegionsObject> Data { get; set; } = new List<MeasureTypeRegionsObject>();
}
