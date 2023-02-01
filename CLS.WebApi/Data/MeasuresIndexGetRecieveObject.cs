namespace CLS.WebApi.Data;

public class MeasuresIndexGetRecieveObject
{
	public int hierarchyId { set; get; }
	public int measureTypeId { set; get; }
	public string owner { set; get; }
	public ErrorModel error { set; get; }
	public MeasureTypeRegionsObject hierarchy { set; get; }
}
