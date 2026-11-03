namespace Marketplace.Api.Data;

public class Reviews
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;

    public virtual Products? Products { get; set; }
    public virtual Users? Users { get; set; }
}
