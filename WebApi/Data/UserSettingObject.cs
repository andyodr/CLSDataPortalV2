namespace Deliver.WebApi.Data;

public sealed class UserSettingObject
{
	public int Id { get; set; }

	public string UserName { get; set; } = null!;

	public ICollection<Lock>? Locks { get; set; }
}
