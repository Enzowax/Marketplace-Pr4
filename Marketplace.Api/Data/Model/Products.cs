namespace Marketplace.Api.Data;

public class Products
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int PriceKopecks { get; set; }
    public int Stock { get; set; }
    public int? PackSize { get; set; }
    public int IsAvailable { get; set; }

    public virtual ICollection<CartItems> CartItems { get; set; } = new HashSet<CartItems>();
    public virtual Categories? Categories { get; set; }
    public virtual ICollection<OrderItems> OrderItems { get; set; } = new HashSet<OrderItems>();
    public virtual ICollection<Reviews> Reviews { get; set; } = new HashSet<Reviews>();
    public virtual ICollection<Users> Users { get; set; } = new HashSet<Users>();
}
