namespace CLS.WebApi.Data;

public class TargetGetAllObject
{
	public int HierarchyId { get; set; }

	public long? MeasureId { get; set; }

	public int MeasureTypeId { get; set; }

	public double? Target { get; set; }

	public double? Yellow { get; set; }

	public bool? ApplyToChildren { get; set; }

	public bool? IsCurrentUpdate { get; set; }

	public TargetConfirmInterval ConfirmIntervals { get; set; } = null!;
}
