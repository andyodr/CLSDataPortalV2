using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class MeasureDefinitionIndexReturnObject
{
	public ErrorModel? Error { get; set; }

	public FilterSaveObject? Filter { get; set; }

	public List<UnitsObject> Units { get; set; } = null!;

	public List<IntervalsObject> Intervals { get; set; } = null!;

	public List<MeasureType> MeasureTypes { get; set; } = null!;

	public List<AggregationFunction> AggFunctions { get; set; } = null!;

	public List<MeasureDefinitionEdit>? Data { get; set; }
}
