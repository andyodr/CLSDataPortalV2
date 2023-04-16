namespace Deliver.WebApi.Data;

public class SettingsGetRecieveObject
{
	public int Year { get; set; }

	public int? NumberOfDays { get; set; }

	public int? CalculateHH { get; set; }

	public int? CalculateMM { get; set; }

	public int? CalculateSS { get; set; }

	public bool? Active { get; set; }

	public List<CalendarLock>? Locked { get; set; }

	public UserSettingObject? Users { get; set; }
}
