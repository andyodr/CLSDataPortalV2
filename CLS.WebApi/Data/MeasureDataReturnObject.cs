namespace CLS.WebApi.Data;

public class MeasureDataReturnObject
{
	public long id { set; get; }
	public string name { set; get; }
	public double? value { set; get; }
	public string explanation { set; get; }
	public string action { set; get; }
	public double? target { set; get; }
	public int? targetCount { get; set; }
	public long? targetId { get; set; }
	public int unitId { set; get; }
	public string units { set; get; }
	public double? yellow { set; get; }
	public string? expression { set; get; }
	public string evaluated { set; get; }
	public bool calculated { set; get; }
	public string? description { set; get; }
	public UpdatedObject updated { set; get; }
}
