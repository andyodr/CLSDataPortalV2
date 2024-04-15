namespace Deliver.WebApi.Data;

public sealed class SettingsGetResponse
{
	public ErrorModel Error { get; set; } = null!;

	public List<int>? Years { get; set; }

	public int Year { get; set; }

	public int? CalculateHH { get; set; }

	public int? CalculateMM { get; set; }

	public int? CalculateSS { get; set; }

	public bool? Active { get; set; }

	public string? LastCalculatedOn { get; set; }

	public List<CalendarLock>? Locked { get; set; }

	public List<UserSettingObject> Users { get; set; } = null!;
}
