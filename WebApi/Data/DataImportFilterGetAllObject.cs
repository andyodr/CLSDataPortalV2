namespace Deliver.WebApi.Data;

public sealed class DataImportFilterGetAllObject
{
	public ErrorModel? Error { get; set; }

	public int Id { get; set; }

	public int? Number { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? EndDate { get; set; }

	public string? Month { get; set; }
}
