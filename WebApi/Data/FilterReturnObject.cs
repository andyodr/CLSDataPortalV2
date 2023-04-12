using CLS.WebApi.Controllers.MeasureDefinition.Type;

namespace CLS.WebApi.Data;

public class FilterReturnObject
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
