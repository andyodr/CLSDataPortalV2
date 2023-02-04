namespace CLS.WebApi.Data.Models;

public class MeasureType
{
	public int Id { set; get; }

	public string Name { set; get; } = null!;

	public string Description { set; get; } = null!;

	public DateTime LastUpdatedOn { set; get; }
}
