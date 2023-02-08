namespace CLS.WebApi.Data;

public class ErrorModel
{
	public long id { get; set; }

	public string message = null;

	public bool authError = false;
}
