namespace CLS.WebApi.Data;

public class MeasureDefinitionIndexReturnObject
{
	public ErrorModel? Error { get; set; }

	public FilterSaveObject? Filter { get; set; }

	public List<UnitsObject> Units { get; set; } = null!;

	public List<IntervalsObject> Intervals { get; set; } = null!;

	public List<MeasureTypeFilterObject> MeasureTypes { get; set; } = null!;

	public List<AggregationFunction> AggFunctions { get; set; } = null!;

	public List<MeasureDefinitionViewModel>? Data { get; set; }
}
