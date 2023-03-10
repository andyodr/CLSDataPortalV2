namespace CLS.WebApi.Data.Models;

public class Setting
{
	public int Id { set; get; }

	public short? NumberOfDays { set; get; } = null;

	public string? CalculateSchedule { set; get; }

	public bool? Active { set; get; } = null;

	public DateTime? LastCalculatedOn { set; get; }

	public DateTime LastUpdatedOn { set; get; }
}
