namespace Deliver.WebApi.Data;

public sealed class DataImportObject
{
	public int Id { get; set; }

	public string Name { get; set; } = null!;

	public IList<HeadingObject> Heading { get; set; } = null!;
}
