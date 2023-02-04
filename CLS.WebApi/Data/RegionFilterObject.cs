namespace CLS.WebApi.Data;

public class RegionFilterObject
{
	public string hierarchy { set; get; }
	public int id { set; get; }
	public int? count { set; get; }
	public ICollection<RegionFilterObject> sub { set; get; }
	public bool? found { get; set; }
	public ErrorModel error { set; get; }
}
