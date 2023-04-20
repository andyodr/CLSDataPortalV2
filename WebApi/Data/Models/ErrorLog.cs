namespace Deliver.WebApi.Data.Models;

public sealed class ErrorLog
{
	/// <summary>
	/// The unique id and primary key for this ErrorLog
	/// </summary>
	public int Id { get; set; }

	public string ErrorMessage { get; set; } = null!;

	public string ErrorMessageDetailed { get; set; } = null!;

	public string StackTrace { get; set; } = null!;
}
