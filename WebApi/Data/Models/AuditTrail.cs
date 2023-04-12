namespace CLS.WebApi.Data.Models;

public class AuditTrail
{
	/// <summary>
	/// The unique id and primary key for this AuditTrail
	/// </summary>
	public long Id { get; set; }

	public string? Type { get; set; }

	public string? Code { get; set; }

	public string? Description { get; set; }

	public string? Data { get; set; }

	public int? UpdatedBy { get; set; }

	public DateTime LastUpdatedOn { get; set; }
}
