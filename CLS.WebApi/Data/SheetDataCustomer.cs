namespace CLS.WebApi.Data;

public class SheetDataCustomer
{
	public int? HierarchyId { get; set; }

	public int? CalendarId { get; set; }

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

	public Byte IsProcessed { get; set; }

	public int rowNumber { get; set; }

	public string HeaderStatusCode { get; set; } = null!;

	public string HeaderStatus { get; set; } = null!;

	public string BlockCode { get; set; } = null!;

	public string BlockText { get; set; } = null!;

	public string RejectionCode { get; set; } = null!;

	public string RejectionText { get; set; } = null!;

	public string CreditStatusCheck { get; set; } = null!;

	public string CreditCode { get; set; } = null!;
}
