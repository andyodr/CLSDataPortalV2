namespace CLS.WebApi.Data;

public class GetIntervalsObject
{
	public ErrorModel error { set; get; }
	public int id { set; get; }
	public byte? number { set; get; }
	public string? startDate { set; get; }
	public string? endDate { set; get; }
	public string? month { set; get; }
}
