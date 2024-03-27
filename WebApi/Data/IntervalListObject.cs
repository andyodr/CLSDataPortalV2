using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class IntervalListObject
{
	public int CalendarId { get; set; }

	public List<GetIntervalsObject> Data { get; init; } = [];

	public List<MeasureType> MeasureTypes { get; init; } = [];
}
