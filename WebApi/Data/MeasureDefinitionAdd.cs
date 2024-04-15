namespace Deliver.WebApi.Data;

public class MeasureDefinitionAdd
{
	public string Name { get; init; } = null!;

	public int MeasureTypeId { get; init; }

	public string? Interval { get; init; }

	public int IntervalId { get; init; }

	public string VarName { get; init; } = null!;

	public string? Description { get; init; }

	public string? Expression { get; set; }

	public byte Precision { get; init; }

	public int Priority { get; init; }

	public short FieldNumber { get; init; }

	public int UnitId { get; init; }

	public string? Units { get; init; }

	public bool? Calculated { get; set; }

	public bool? Daily { get; set; }

	public bool? Weekly { get; set; }

	public bool? Monthly { get; set; }

	public bool? Quarterly { get; set; }

	public bool? Yearly { get; set; }

	public string? AggFunction { get; set; }

	public byte? AggFunctionId { get; set; }
}

public sealed class MeasureDefinitionEdit : MeasureDefinitionAdd {

	public MeasureDefinitionEdit() { }

	public MeasureDefinitionEdit(MeasureDefinitionAdd copy) {
		Name = copy.Name;
		MeasureTypeId = copy.MeasureTypeId;
		Interval = copy.Interval;
		IntervalId = copy.IntervalId;
		VarName = copy.VarName;
		Description = copy.Description;
		Expression = copy.Expression;
		Precision = copy.Precision;
		Priority = copy.Priority;
		FieldNumber = copy.FieldNumber;
		UnitId = copy.UnitId;
		Units = copy.Units;
		Calculated	= copy.Calculated;
		Daily = copy.Daily;
		Weekly = copy.Weekly;
		Monthly = copy.Monthly;
		Quarterly = copy.Quarterly;
		Yearly = copy.Yearly;
		AggFunction = copy.AggFunction;
		AggFunctionId = copy.AggFunctionId;
	}

	public long Id { get; set; }
}
