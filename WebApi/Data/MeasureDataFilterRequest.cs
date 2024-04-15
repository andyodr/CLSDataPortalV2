namespace Deliver.WebApi.Data;

public sealed class MeasureDataFilterRequest
{
	public int? IntervalId { get; init; }

	public int? Year { get; init; }

	public bool? IsDataImport { get; init; }
}
