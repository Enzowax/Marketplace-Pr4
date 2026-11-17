namespace Marketplace.Api.Data;

public class PickupPoints
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public int IsActive { get; set; }

    public virtual ICollection<Orders> Orders { get; set; } = new HashSet<Orders>();
}
