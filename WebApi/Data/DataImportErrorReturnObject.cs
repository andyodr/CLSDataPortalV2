namespace Deliver.WebApi.Data;

public sealed class DataImportErrorReturnObject
{
	public long? Id;

	public int? Row { get; set; }

	public string Message { get; set; } = null!;
}
