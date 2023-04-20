namespace Deliver.WebApi.Data;

public sealed class MeasureIDReturnObject
{
	public IList<MeasureTypeDataObject> Data { get; set; } = new List<MeasureTypeDataObject>();

	public ErrorModel? Error { get; set; }
}
