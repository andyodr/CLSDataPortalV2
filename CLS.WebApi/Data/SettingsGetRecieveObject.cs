namespace CLS.WebApi.Data;

public class SettingsGetRecieveObject
{
	public int year { set; get; }

	public int? numberOfDays { set; get; }

	public int? calculateHH { get; set; }

	public int? calculateMM { get; set; }

	public int? calculateSS { get; set; }

	public bool? active { set; get; }

	public List<CalendarLock> locked { set; get; }

	public UserSettingObject users { set; get; }
}
