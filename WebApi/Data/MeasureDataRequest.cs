namespace Deliver.WebApi.Data;

public sealed class MeasureDataRequest
{
	public int CalendarId { get; init; }

	public string? Day { get; init; }

	public int HierarchyId { get; init; }

	public int MeasureTypeId { get; init; }

	public long? MeasureDataId { get; init; }

	public double? MeasureValue { get; init; }

	public string? Explanation { get; init; } = null!;

	public string? Action { get; init; } = null!;
}
