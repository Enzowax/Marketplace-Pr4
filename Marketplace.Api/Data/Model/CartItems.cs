namespace Marketplace.Api.Data;

public class CartItems
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public virtual Products? Products { get; set; }
    public virtual Users? Users { get; set; }
}
