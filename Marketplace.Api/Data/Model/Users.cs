namespace Marketplace.Api.Data;

public class Users
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public virtual ICollection<CartItems> CartItems { get; set; } = new HashSet<CartItems>();
    public virtual ICollection<Orders> Orders { get; set; } = new HashSet<Orders>();
    public virtual ICollection<Reviews> Reviews { get; set; } = new HashSet<Reviews>();
    public virtual ICollection<Products> Products { get; set; } = new HashSet<Products>();
}
