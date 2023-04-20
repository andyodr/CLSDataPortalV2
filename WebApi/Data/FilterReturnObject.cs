using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class FilterReturnObject
{
	public IList<MeasureType> MeasureTypes { get; set; } = null!;

	public IList<RegionFilterObject>? Hierarchy { get; set; }

	public IList<IntervalsObject>? Intervals { get; set; }

	public IList<YearsObject>? Years { get; set; }

	public ErrorModel? Error { get; set; }

	public FilterSaveObject Filter { get; set; } = null!;

	public CurrentCalendars? CurrentCalendarIds { get; set; }

	public RegionIndexGetReturnObject Measures { get; set; } = null!;
}
