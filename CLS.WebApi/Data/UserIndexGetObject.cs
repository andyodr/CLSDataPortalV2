namespace CLS.WebApi.Data;

public class UserIndexGetObject
{
	public List<IntervalsObject> roles { set; get; }

	public ErrorModel error { set; get; }

	public List<RegionFilterObject>? hierarchy { set; get; }

	public List<UserIndexDto> data { set; get; }
}
