namespace CLS.WebApi.Data;

public class SheetDataCustomer
{
	public int? HierarchyId { set; get; }
	public int? CalendarId { set; get; }
	public string CustomerGroup { set; get; }
	public string CustomerSubGroup { set; get; }
	public string PurchaseType { set; get; }
	public string TradeChannel { set; get; }
	public string TradeChannelGroup { set; get; }
	public double? Sales { set; get; }
	public double? NumOrders { set; get; }
	public double? NumLines { set; get; }
	public string OrderType { get; set; }
	public double? NumLateOrders { get; set; }
	public double? NumLateLines { get; set; }
	public double? NumOrdLens { get; set; }
	public double? OrdQty { get; set; }
	public Byte IsProcessed { get; set; }
	public int rowNumber { get; set; }
	public string HeaderStatusCode { set; get; }
	public string HeaderStatus { set; get; }
	public string BlockCode { set; get; }
	public string BlockText { set; get; }
	public string RejectionCode { set; get; }
	public string RejectionText { set; get; }
	public string CreditStatusCheck { set; get; }
	public string CreditCode { set; get; }
}
