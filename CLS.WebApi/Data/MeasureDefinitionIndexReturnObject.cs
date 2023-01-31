namespace CLS.WebApi.Data;

public class MeasureDefinitionIndexReturnObject
{
	public ErrorModel error { set; get; }
	public FilterSaveObject filter { set; get; }
	public List<UnitsObject> units { set; get; }
	public List<IntervalsObject> intervals { set; get; }
	public List<MeasureTypeFilterObject> measureTypes { set; get; }
	public List<AggregationFunction> aggFunctions { get; set; }
	public List<MeasureDefinitionViewModel> data { set; get; }
}
