namespace Deliver.WebApi.Data;

public sealed class MeasuresIndexPutObject
{
	public ErrorModel? Error { get; set; }

	public long MeasureDefinitionId { get; set; }

	public IList<RegionActiveCalculatedObject> Hierarchy { get; set; } = null!;
}
