namespace Deliver.WebApi.Data;

public sealed class MeasureDataReturnObject
{
	public long Id { get; set; }

	public string Name { get; set; } = null!;
	public string? Description { get; set; }

	public double? Value { get; set; }

	public string? Explanation { get; set; }

	public string? Action { get; set; }

	public double? Target { get; set; }

	public int? TargetCount { get; set; }

	public long? TargetId { get; set; }

	public int UnitId { get; set; }

	public string Units { get; set; } = null!;

	public double? Yellow { get; set; }
	public bool Calculated { get; set; }

	public string VariableName { get; set; } = null!;

	public string? Expression { get; set; }

	public string Evaluated { get; set; } = null!;



	public UpdatedObject Updated { get; set; } = null!;
}
