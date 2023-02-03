namespace CLS.WebApi.Data;

public class TargetGetAllObject
{
	public int hierarchyId { set; get; }

	public long? measureId { set; get; }

	public int measureTypeId { set; get; }

	public double? target { set; get; }

	public double? yellow { set; get; }

	public bool? applyToChildren { set; get; }

	public bool? isCurrentUpdate { get; set; }

	public TargetConfirmInterval confirmIntervals { get; set; }
}
