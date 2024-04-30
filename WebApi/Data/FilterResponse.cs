using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class FilterResponse
{
	public IReadOnlyList<MeasureType> MeasureTypes { get; set; } = null!;

	public IReadOnlyList<RegionFilterObject>? Hierarchy { get; set; }

	public IList<IntervalDto>? Intervals { get; set; }

	public IList<YearsDto>? Years { get; set; }

	public ErrorModel? Error { get; set; }

	public FilterSaveDto Filter { get; set; } = null!;

	public CurrentCalendars? CurrentCalendarIds { get; set; }

	public RegionIndexGetResponse Measures { get; set; } = null!;
}
