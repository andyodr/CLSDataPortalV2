using Deliver.WebApi.Controllers.MeasureDefinition.Type;

namespace Deliver.WebApi.Data;

public sealed class FiltersIntervalsResponse
{
	public int CalendarId { get; set; }

	public IList<GetIntervalsResponse> Data { get; init; } = [];

	public IList<MeasureType> MeasureTypes { get; init; } = [];
}
