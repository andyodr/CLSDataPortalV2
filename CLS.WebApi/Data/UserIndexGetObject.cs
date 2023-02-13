namespace CLS.WebApi.Data;

public class UserIndexGetObject
{
	public List<IntervalsObject> roles { get; set; } = null!;

	public ErrorModel error { get; set; } = null!;

	public List<RegionFilterObject>? hierarchy { get; set; } = null!;

	public List<UserIndexDto> data { get; set; } = null!;
}
