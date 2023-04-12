namespace CLS.WebApi.Data.Models;

public class AuditTrail
{
	/// <summary>
	/// The unique id and primary key for this AuditTrail
	/// </summary>
	public long Id { set; get; }

	public string? Type { set; get; }

	public string? Code { set; get; }

	public string? Description { set; get; }

	public string? Data { set; get; }

	public int? UpdatedBy { get; set; } = null;

	public DateTime LastUpdatedOn { set; get; }
}
