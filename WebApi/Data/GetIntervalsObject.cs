namespace Deliver.WebApi.Data;

public class GetIntervalsObject
{
	public ErrorModel Error { get; set; } = new();

	public int Id { get; set; }

	public byte? Number { get; set; }

	public string? StartDate { get; set; }

	public string? EndDate { get; set; }

	public string? Month { get; set; }
}
