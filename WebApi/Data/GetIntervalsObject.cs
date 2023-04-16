namespace Deliver.WebApi.Data;

public class GetIntervalsObject
{
	public ErrorModel error { get; set; } = new();

	public int id { get; set; }

	public byte? number { get; set; }

	public string? startDate { get; set; }

	public string? endDate { get; set; }

	public string? month { get; set; }
}
