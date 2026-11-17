namespace Marketplace.Api.Data;

public class OrderItems
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int UnitPriceKopecks { get; set; }

    public virtual Orders? Orders { get; set; }
    public virtual Products? Products { get; set; }
}
