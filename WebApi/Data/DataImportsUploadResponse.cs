namespace Deliver.WebApi.Data;

public sealed class DataImportsUploadResponse
{
	public DataImportsResponse? Data { get; set; }

	public List<ImportErrorResult> Error { get; set; } = null!;
}
