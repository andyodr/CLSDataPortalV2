using CLS.WebApi.Controllers.MeasureDefinition.Type;

namespace CLS.WebApi.Data;

public class FilterReturnObject
{
	public IList<MeasureType> MeasureTypes { get; set; } = null!;

	public List<RegionFilterObject>? Hierarchy { get; set; }

	public List<IntervalsObject>? Intervals { get; set; }

	public List<YearsObject>? Years { get; set; }

	public ErrorModel? Error { get; set; }

	public FilterSaveObject Filter { get; set; } = null!;

	public CurrentCalendars? CurrentCalendarIds { get; set; }

	public RegionIndexGetReturnObject Measures { get; set; } = null!;
}
