namespace Deliver.WebApi.Data;

public sealed class FilterSaveObject
{
	public int? hierarchyId { set; get; }
	public int? measureTypeId { set; get; }
	public int? intervalId { set; get; }
	public int? calendarId { set; get; }
	public int? year { set; get; }
}
