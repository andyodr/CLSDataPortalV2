namespace Deliver.WebApi.Data;

public class UserIndexGetObject
{
	public List<IntervalsObject> Roles { get; set; } = null!;

	public ErrorModel Error { get; set; } = null!;

	public List<RegionFilterObject>? Hierarchy { get; set; } = null!;

	public IList<UserIndexDto> Data { get; set; } = null!;
}
