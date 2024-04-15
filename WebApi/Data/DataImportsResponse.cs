namespace Deliver.WebApi.Data;

public sealed class DataImportsResponse
{
	public DataImportsResponseDataElement? Data { get; set; }

	public List<ImportErrorResult> Error { get; set; } = null!;
}
