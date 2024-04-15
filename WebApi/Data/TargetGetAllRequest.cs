namespace Deliver.WebApi.Data;

public record TargetConfirmInterval(bool? Daily, bool? Weekly, bool? Monthly, bool? Quarterly, bool? Yearly);

public class TargetGetAllRequest
{
	public int HierarchyId { get; init; }

	public long? MeasureId { get; init; }

	public int MeasureTypeId { get; init; }

	public double? Target { get; init; }

	public double? Yellow { get; init; }

	public bool? ApplyToChildren { get; init; }

	public bool? IsCurrentUpdate { get; init; }

	public TargetConfirmInterval ConfirmIntervals { get; init; } = null!;
}
