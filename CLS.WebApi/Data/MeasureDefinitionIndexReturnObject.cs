namespace CLS.WebApi.Data;

public class MeasureDefinitionIndexReturnObject
{
	public ErrorModel error { get; set; }

	public FilterSaveObject filter { get; set; }

	public List<UnitsObject> units { get; set; }

	public List<IntervalsObject> intervals { get; set; }

	public List<MeasureTypeFilterObject> measureTypes { get; set; }

	public List<AggregationFunction> aggFunctions { get; set; }

	public List<MeasureDefinitionViewModel>? Data { get; set; }
}
