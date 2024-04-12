using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class IntervalListObject
{
	public int CalendarId { get; set; }

	public IList<GetIntervalsObject> Data { get; init; } = [];

	public IList<MeasureType> MeasureTypes { get; init; } = [];
}
