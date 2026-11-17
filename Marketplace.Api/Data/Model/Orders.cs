namespace Marketplace.Api.Data;

public class Orders
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalKopecks { get; set; }
    public int? PickupPointId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual ICollection<OrderItems> OrderItems { get; set; } = new HashSet<OrderItems>();
    public virtual PickupPoints? PickupPoints { get; set; }
    public virtual Users? Users { get; set; }
}
