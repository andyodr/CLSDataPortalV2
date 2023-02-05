namespace CLS.WebApi.Data;

public class MeasureDefinitionViewModel
{
	public long? id { get; set; }
	public string name { get; set; }
	public int measureTypeId { set; get; }
	public string interval { set; get; }
	public int intervalId { set; get; }
	public string varName { set; get; }
	public string? description { set; get; }
	public string? expression { set; get; }
	public byte precision { set; get; }
	public int priority { set; get; }
	public short fieldNumber { get; set; }
	public int unitId { set; get; }
	public string units { set; get; }
	public bool? calculated { set; get; }
	public bool? daily { set; get; }
	public bool? weekly { set; get; }
	public bool? monthly { set; get; }
	public bool? quarterly { set; get; }
	public bool? yearly { set; get; }
	public string aggFunction { get; set; }
	public byte? aggFunctionId { get; set; }
}
