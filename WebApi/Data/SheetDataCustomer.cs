namespace Deliver.WebApi.Data;

public sealed class SheetDataCustomer
{
	public int? HierarchyId { get; init; }

	public int? CalendarId { get; init; }

	public string CustomerGroup { get; init; } = null!;

	public string CustomerSubGroup { get; init; } = null!;

	public string PurchaseType { get; init; } = null!;

	public string TradeChannel { get; init; } = null!;

	public string TradeChannelGroup { get; init; } = null!;

	public double? Sales { get; init; }

	public double? NumOrders { get; init; }

	public double? NumLines { get; init; }

	public string OrderType { get; init; } = null!;

	public double? NumLateOrders { get; init; }

	public double? NumLateLines { get; init; }

	public double? NumOrdLens { get; init; }

	public double? OrdQty { get; init; }

	public Byte IsProcessed { get; init; }

	public int rowNumber { get; init; }

	public string HeaderStatusCode { get; init; } = null!;

	public string HeaderStatus { get; init; } = null!;

	public string BlockCode { get; init; } = null!;

	public string BlockText { get; init; } = null!;

	public string RejectionCode { get; init; } = null!;

	public string RejectionText { get; init; } = null!;

	public string CreditStatusCheck { get; init; } = null!;

	public string CreditCode { get; init; } = null!;
}
