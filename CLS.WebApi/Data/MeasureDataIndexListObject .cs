namespace CLS.WebApi.Data;

public class MeasureDataIndexListObject
{
	public string Range { get; set; } = null!;

	public int? CalendarId { get; set; }

	public bool Allow { get; set; }

	public bool EditValue { get; set; }

	public bool Locked { get; set; }

	public bool Confirmed { get; set; }

	public FilterSaveObject Filter { get; set; } = null!;

	public List<MeasureDataReturnObject> Data { get; set; } = null!;

	public ErrorModel Error { get; set; } = null!;
}
