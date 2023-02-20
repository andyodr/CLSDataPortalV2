namespace CLS.WebApi.Data;

public class MeasureDataReceiveObject
{
	public int? CalendarId { get; set; }

	public string Day { get; set; } = null!;

	public int HierarchyId { get; set; }

	public int MeasureTypeId { get; set; }

	public long? MeasureDataId { get; set; }

	public double? MeasureValue { get; set; }

	public string Explanation { get; set; } = null!;

	public string Action { get; set; } = null!;
}
