using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class MeasureDefinitionIndexReturnObject
{
	public ErrorModel? Error { get; set; }

	public FilterSaveObject? Filter { get; set; }

	public IList<UnitsObject> Units { get; set; } = null!;

	public IList<IntervalsObject> Intervals { get; set; } = null!;

	public IList<MeasureType> MeasureTypes { get; set; } = null!;

	public IList<AggregationFunction> AggFunctions { get; set; } = null!;

	public IList<MeasureDefinitionEdit>? Data { get; set; }
}
