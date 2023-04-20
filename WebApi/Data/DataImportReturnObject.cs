namespace Deliver.WebApi.Data;

public sealed class DataImportReturnObject
{
	public DataImportsMainObject? Data { get; set; }

	public List<DataImportErrorReturnObject> Error { get; set; } = null!;
}
