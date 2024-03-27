using System.Text.Json.Serialization;

namespace Deliver.WebApi.Data;

public sealed class GetIntervalsObject
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ErrorModel? Error { get; set; }

	public int Id { get; set; }

	public byte? Number { get; set; }

	public string? StartDate { get; set; }

	public string? EndDate { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Month { get; set; }
}
