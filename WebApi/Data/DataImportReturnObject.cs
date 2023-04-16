namespace Deliver.WebApi.Data;

public class DataImportReturnObject
{
	public DataImportsMainObject? Data { get; set; }

	public List<DataImportErrorReturnObject> Error { get; set; } = null!;
}
