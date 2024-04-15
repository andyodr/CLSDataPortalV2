namespace Deliver.WebApi.Data;

public sealed class MeasuresIndexRequest
{
	public ErrorModel? Error { get; set; }

	public long MeasureDefinitionId { get; init; }

	public IList<RegionActiveCalculatedObject> Hierarchy { get; init; } = null!;
}
