namespace Deliver.WebApi.Data;

public sealed class ErrorModel
{
	public long Id { get; set; }

	public string Message = null!;

	public bool AuthError = false;
}
