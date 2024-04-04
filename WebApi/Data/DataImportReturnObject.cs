namespace Deliver.WebApi.Data;

public sealed class DataImportReturnObject
{
	public DataImportsMainObject? Data { get; set; }

	public List<ImportErrorResult> Error { get; set; } = null!;
}
