namespace Deliver.WebApi.Data;

public sealed class DataImportFilterGetAllResponse
{
	public ErrorModel? Error { get; init; }

	public int Id { get; init; }

	public int? Number { get; init; }

	public DateTime? StartDate { get; init; }

	public DateTime? EndDate { get; init; }

	public string? Month { get; init; }
}
