namespace CLS.WebApi.Data.Models;

public class CustomerHierarchy
{
	/// <summary>
	/// The unique id and primary key for this CustomerHierarchy
	/// </summary>
	public int Id { get; set; }

	public int HierarchyId { get; set; }

	public Hierarchy? Hierarchy { get; set; }

	public int CalendarId { get; set; }

	public Calendar? Calendar { get; set; }

	public string CustomerGroup { get; set; } = null!;

	public string CustomerSubGroup { get; set; } = null!;

	public string PurchaseType { get; set; } = null!;

	public string TradeChannel { get; set; } = null!;

	public string TradeChannelGroup { get; set; } = null!;

	public double? Sales { get; set; }

	public double? NumOrders { get; set; }

	public double? NumLines { get; set; }

	public string OrderType { get; set; } = null!;

	public double? NumLateOrders { get; set; }

	public double? NumLateLines { get; set; }

	public double? NumOrdLens { get; set; }

	public double? OrdQty { get; set; }

	public byte IsProcessed { get; set; }

	public string? HeaderStatusCode { get; set; }

	public string? HeaderStatus { get; set; }

	public string? BlockCode { get; set; }

	public string? BlockText { get; set; }

	public string? RejectionCode { get; set; }

	public string? RejectionText { get; set; }

	public string? CreditStatusCheck { get; set; }

	public string? CreditCode { get; set; }
}
