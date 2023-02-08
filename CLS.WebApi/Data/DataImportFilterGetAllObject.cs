namespace CLS.WebApi.Data;

public class DataImportFilterGetAllObject
{
	public ErrorModel error { get; set; }
	public int id { get; set; }
	public int? number { get; set; }
	public DateTime? startDate { get; set; }
	public DateTime? endDate { get; set; }
	public string? month { get; set; }
}
