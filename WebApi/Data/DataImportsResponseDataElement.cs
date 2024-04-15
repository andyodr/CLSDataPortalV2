namespace Deliver.WebApi.Data;

public sealed class DataImportsResponseDataElement
{
	public ErrorModel? Error { get; set; }

	public string CalculationTime { get; set; } = null!;

	public IReadOnlyList<IntervalDto> Intervals { get; set; } = null!;

	public IReadOnlyList<YearsDto> Years { get; set; } = null!;

	public IList<DataImportsResponseDataImportElement> DataImport { get; set; } = null!;

	public int? IntervalId { get; set; }

	public int? CalendarId { get; set; }

	public int? CurrentYear { get; set; }
}
