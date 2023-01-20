namespace CLS.WebApi.Data.Models;

public class CustomerHierarchy
{
	/// <summary>
	/// The unique id and primary key for this CustomerHierarchy
	/// </summary>
	public int Id { set; get; }

	public Hierarchy? Hierarchy { set; get; } = null;

	public Calendar? Calendar { set; get; } = null;

	public string CustomerGroup { set; get; } = null!;

	public string CustomerSubGroup { set; get; } = null!;

	public string PurchaseType { set; get; } = null!;

	public string TradeChannel { set; get; } = null!;

	public string TradeChannelGroup { set; get; } = null!;

	public double? Sales { set; get; } = null;

	public double? NumOrders { set; get; } = null;

	public double? NumLines { set; get; } = null;

	public string OrderType { get; set; } = null!;

	public double? NumLateOrders { set; get; } = null;

	public double? NumLateLines { set; get; } = null;

	public double? NumOrdLens { set; get; } = null;

	public double? OrdQty { set; get; } = null;

	public byte IsProcessed { get; set; }
}
