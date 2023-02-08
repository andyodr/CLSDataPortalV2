namespace CLS.WebApi.Data;

public class UserSettingObject
{
	public int Id { set; get; }
	public string UserName { set; get; } = null!;
	public ICollection<Lock>? Locks { set; get; }
}
