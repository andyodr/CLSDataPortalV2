namespace CLS.WebApi.Data;

public class MeasureIDReturnObject
{
	public IList<MeasureTypeDataObject> Data { get; set; } = new List<MeasureTypeDataObject>();

	public ErrorModel? Error { get; set; }
}
