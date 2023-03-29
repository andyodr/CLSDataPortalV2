namespace CLS.WebApi.Data;

public class MeasureDataIndexListObject
{
	public string Range { get; set; }

	public int? CalendarId { get; set; }

	public bool Allow { get; set; }

	public bool EditValue { get; set; }

	public bool Locked { get; set; }

	public bool Confirmed { get; set; }

	public FilterSaveObject Filter { get; set; }

	public List<MeasureDataReturnObject> Data { get; set; }

	public ErrorModel Error { get; set; } = null!;
}
