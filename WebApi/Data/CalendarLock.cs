namespace Deliver.WebApi.Data;

public class CalendarLock
{
	public int Id { get; set; }

	public string? Month { get; set; }

	public string? StartDate { get; set; }

	public string? EndDate { get; set; }

	public bool? Locked { get; set; }
}
