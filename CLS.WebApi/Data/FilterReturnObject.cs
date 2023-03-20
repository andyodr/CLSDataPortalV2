namespace CLS.WebApi.Data;

public class FilterReturnObject
{
	public List<MeasureTypeFilterObject> MeasureTypes { get; set; } = null!;

	public List<RegionFilterObject>? Hierarchy { get; set; }

	public List<IntervalsObject>? Intervals { get; set; }

	public List<YearsObject>? Years { get; set; }

	public ErrorModel? Error { get; set; }

	public FilterSaveObject Filter { get; set; } = null!;

	public CurrentCalendars? CurrentCalendarIds { get; set; }
}
