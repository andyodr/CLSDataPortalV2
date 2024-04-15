namespace Deliver.WebApi.Data;

public sealed class MeasureDataIndexResponse
{
	public string Range { get; set; } = null!;

	public int? CalendarId { get; set; }

	public bool Allow { get; set; }

	public bool EditValue { get; set; }

	public bool Locked { get; set; }

	public bool Confirmed { get; set; }

	public FilterSaveDto Filter { get; set; } = null!;

	public IList<MeasureDataResponse> Data { get; set; } = null!;

	public ErrorModel Error { get; set; } = null!;
}
