namespace CLS.WebApi.Data;

public class MeasureDefinitionAdd
{
	public string Name { get; set; } = null!;

	public int MeasureTypeId { get; set; }

	public string? Interval { get; set; }

	public int IntervalId { get; set; }

	public string VarName { get; set; } = null!;

	public string? Description { get; set; }

	public string? Expression { get; set; }

	public byte Precision { get; set; }

	public int Priority { get; set; }

	public short FieldNumber { get; set; }

	public int UnitId { get; set; }

	public string? Units { get; set; }

	public bool? Calculated { get; set; }

	public bool? Daily { get; set; }

	public bool? Weekly { get; set; }

	public bool? Monthly { get; set; }

	public bool? Quarterly { get; set; }

	public bool? Yearly { get; set; }

	public string? AggFunction { get; set; }

	public byte? AggFunctionId { get; set; }
}

public class MeasureDefinitionEdit : MeasureDefinitionAdd {

	public MeasureDefinitionEdit() { }

	public MeasureDefinitionEdit(MeasureDefinitionAdd copy) {
		this.Name = copy.Name;
		this.MeasureTypeId = copy.MeasureTypeId;
		this.Interval = copy.Interval;
		this.IntervalId = copy.IntervalId;
		this.VarName = copy.VarName;
		this.Description = copy.Description;
		this.Expression = copy.Expression;
		this.Precision = copy.Precision;
		this.Priority = copy.Priority;
		this.FieldNumber = copy.FieldNumber;
		this.UnitId= copy.UnitId;
		this.Units = copy.Units;
		this.Calculated	= copy.Calculated;
		this.Daily = copy.Daily;
		this.Weekly = copy.Weekly;
		this.Monthly = copy.Monthly;
		this.Quarterly = copy.Quarterly;
		this.Yearly = copy.Yearly;
		this.AggFunction = copy.AggFunction;
		this.AggFunctionId = copy.AggFunctionId;
	}

	public long Id { get; set; }
}
