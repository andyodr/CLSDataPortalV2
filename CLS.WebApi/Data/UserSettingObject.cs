namespace CLS.WebApi.Data;

public class UserSettingObject
{
	public int id { set; get; }
	public string userName { set; get; }
	public List<Lock> locks { set; get; }
}
