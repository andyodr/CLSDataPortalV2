namespace Deliver.WebApi.Data;

public sealed class MeasureDataFilterReceiveObject
{
	public int? IntervalId { get; set; }

	public int? Year { get; set; }

	public bool? IsDataImport { get; set; }
}
