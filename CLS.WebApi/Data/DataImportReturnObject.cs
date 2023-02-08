namespace CLS.WebApi.Data;

public class DataImportReturnObject
{
	public DataImportsMainObject? data { get; set; }

	public List<DataImportErrorReturnObject> error { get; set; }
}
