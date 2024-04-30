using Deliver.WebApi.Controllers.MeasureDefinition.Type;
using System.Text.Json.Serialization;

namespace Deliver.WebApi.Data;

public sealed class FiltersIntervalsResponse
{
	public int CalendarId { get; set; }

	public IReadOnlyList<GetIntervalsResponse> Data { get; init; } = null!;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<MeasureType>? MeasureTypes { get; init; }
}
