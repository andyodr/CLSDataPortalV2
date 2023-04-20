namespace Deliver.WebApi.Data;

public sealed class UpdatedObject
{
	public string? By { get; set; }

	public string LongDt { get; set; } = null!;

	public string ShortDt { get; set; } = null!;
}
