namespace CLS.WebApi.Data;

public class FilterReturnObject
{
	public List<MeasureTypeFilterObject> measureTypes { set; get; }
	public List<RegionFilterObject> hierarchy { set; get; }
	public List<IntervalsObject> intervals { set; get; }
	public List<YearsObject> years { set; get; }
	public ErrorModel error { set; get; }
	public FilterSaveObject filter { set; get; }
	public CurrentCalendars currentCalendarIds { get; set; }
}
