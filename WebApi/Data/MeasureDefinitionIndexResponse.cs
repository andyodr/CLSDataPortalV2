using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class MeasureDefinitionIndexResponse
{
	public ErrorModel? Error { get; set; }

	public FilterSaveDto? Filter { get; set; }

	public IList<UnitsDto> Units { get; set; } = null!;

	public IList<IntervalDto> Intervals { get; set; } = null!;

	public IList<MeasureType> MeasureTypes { get; set; } = null!;

	public IList<AggregationFunction> AggFunctions { get; set; } = null!;

	public IList<MeasureDefinitionEdit>? Data { get; set; }
}
