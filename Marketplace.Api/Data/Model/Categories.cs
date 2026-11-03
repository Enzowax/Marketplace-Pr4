namespace Marketplace.Api.Data;

public class Categories
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Products> Products { get; set; } = new HashSet<Products>();
}
