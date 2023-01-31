namespace CLS.WebApi.Data;

public class ErrorModel
{
	public long id { set; get; }
	public string message = null;
	public bool authError = false;
}
