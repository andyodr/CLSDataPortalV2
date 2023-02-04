namespace CLS.WebApi.Data.Models;

public class ErrorLog
{
	/// <summary>
	/// The unique id and primary key for this ErrorLog
	/// </summary>
	public int Id { set; get; }

	public string ErrorMessage { set; get; } = null!;

	public string ErrorMessageDetailed { set; get; } = null!;

	public string StackTrace { set; get; } = null!;
}
