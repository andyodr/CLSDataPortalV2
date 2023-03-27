namespace CLS.WebApi.Data;

public class UserSettingObject
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public ICollection<Lock>? Locks { get; set; }
}
