namespace CLS.WebApi.Data;

public class MeasureIDReturnObject
{
	public ICollection<MeasureTypeDataObject> data { set; get; } = new List<MeasureTypeDataObject>();
	public ErrorModel error { set; get; }
}
