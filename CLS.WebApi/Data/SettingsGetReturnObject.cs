namespace CLS.WebApi.Data;

public class SettingsGetReturnObject
{
	public ErrorModel error { set; get; }
	public List<int> years { set; get; }
	public int year { set; get; }
	//public int? numberOfDays { set; get; }
	public int? calculateHH { get; set; }
	public int? calculateMM { get; set; }
	public int? calculateSS { get; set; }
	public bool? active { set; get; }
	public string lastCalculatedOn { set; get; }
	public List<CalendarLock> locked { set; get; }
	public List<UserSettingObject> users { set; get; }
}
