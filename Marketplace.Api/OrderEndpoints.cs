using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Security.Claims;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        app.MapGet("/api/pickup-points", async (MarketplaceEntities db, CancellationToken ct) =>
            await db.PickupPoints.AsNoTracking().Where(point => point.IsActive == 1)
                .OrderBy(point => point.Address).Select(point => new { point.Id, point.Address }).ToListAsync(ct));

        app.MapPost("/api/orders", async (Checkout input, ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
        {
            if (input.ProductId is <= 0 || (input.ProductId.HasValue && input.Quantity <= 0)
                || input.PickupPointId.HasValue == !string.IsNullOrWhiteSpace(input.DeliveryAddress)
                || input.DeliveryAddress?.Length > 512)
                return Results.BadRequest(new { error = "Проверьте товары и способ получения." });

            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                var cart = input.ProductId.HasValue ? [] : await db.CartItems
                    .Where(item => item.UserId == userId).ToListAsync(ct);
                var quantities = input.ProductId.HasValue
                    ? new Dictionary<int, int> { [input.ProductId.Value] = input.Quantity }
                    : cart.ToDictionary(item => item.ProductId, item => item.Quantity);
                if (quantities.Count == 0)
                    return Results.BadRequest(new { error = "Корзина пуста." });

                var ids = quantities.Keys.ToList();
                var products = await db.Products.Where(product => ids.Contains(product.Id)).OrderBy(product => product.Id).ToListAsync(ct);
                if (products.Count != quantities.Count || products.Any(product =>
                    product.IsAvailable != 1 || product.Stock < quantities[product.Id]))
                    return Results.Conflict(new { error = "Товара нет в нужном количестве. Обновите корзину." });

                var total = products.Sum(product => (long)product.PriceKopecks * quantities[product.Id]);
                if (total > int.MaxValue)
                    return Results.BadRequest(new { error = "Сумма заказа слишком велика." });
                if (input.PickupPointId.HasValue)
                {
                    if (!await db.PickupPoints.AnyAsync(point => point.Id == input.PickupPointId.Value && point.IsActive == 1, ct))
                        return Results.BadRequest(new { error = "Выберите действующий ПВЗ." });
                }
                else if (total <= 60000)
                    return Results.BadRequest(new { error = "Доставка доступна при сумме больше 600 рублей." });

                var order = new Orders
                {
                    UserId = userId, CreatedAt = DateTime.UtcNow, TotalKopecks = (int)total,
                    PickupPointId = input.PickupPointId, DeliveryAddress = input.DeliveryAddress?.Trim(),
                    Status = "Placed"
                };
                // Pickup orders store only the pickup point.
                if (input.PickupPointId.HasValue)
                    order.DeliveryAddress = null;
                foreach (var product in products)
                {
                    var quantity = quantities[product.Id];
                    product.Stock -= quantity;
                    order.OrderItems.Add(new OrderItems
                    {
                        ProductId = product.Id, Quantity = quantity, UnitPriceKopecks = product.PriceKopecks
                    });
                }
                db.Orders.Add(order);
                db.CartItems.RemoveRange(cart);
                await db.SaveChangesAsync(ct);
                transaction.Commit();
                return Results.Created($"/api/orders/{order.Id}", new { order.Id, order.TotalKopecks, order.Status });
            }
            catch (Exception error) when (error is DbUpdateConcurrencyException
                || error is SqlException { Number: 1205 }
                || error is DbUpdateException && error.GetBaseException() is SqlException { Number: 1205 })
            {
                return Results.Conflict(new { error = "Заказ не оформлен. Обновите данные и повторите действие." });
            }
        }).RequireAuthorization();

        app.MapGet("/api/orders", async (ClaimsPrincipal user, MarketplaceEntities db, bool? oldestFirst, CancellationToken ct) =>
        {
            var id = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var query = db.Orders.AsNoTracking().Where(order => order.UserId == id);
            var sorted = oldestFirst == true ? query.OrderBy(order => order.CreatedAt).ThenBy(order => order.Id)
                : query.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id);
            return await sorted.Select(order => new
            {
                order.Id, order.CreatedAt, order.TotalKopecks, order.Status,
                order.DeliveryAddress, order.PickupPointId,
                PickupAddress = order.PickupPoints == null ? null : order.PickupPoints.Address,
                Items = order.OrderItems.Select(item => new { item.ProductId, Name = item.Products!.Name, item.Quantity, item.UnitPriceKopecks })
            }).ToListAsync(ct);
        }).RequireAuthorization();
        app.MapGet("/api/orders/{orderId:int}", async (int orderId, ClaimsPrincipal user,
            MarketplaceEntities db, CancellationToken ct) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await db.Orders.AsNoTracking()
                .Where(item => item.Id == orderId && item.UserId == userId)
                .Select(item => new
                {
                    item.Id, item.CreatedAt, item.TotalKopecks, item.Status,
                    item.DeliveryAddress, item.PickupPointId,
                    PickupAddress = item.PickupPoints == null ? null : item.PickupPoints.Address,
                    Items = item.OrderItems.OrderBy(line => line.ProductId).Select(line => new
                    {
                        line.ProductId, Name = line.Products!.Name, line.Quantity, line.UnitPriceKopecks
                    })
                }).SingleOrDefaultAsync(ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization();
    }

    private sealed record Checkout(int? ProductId, int Quantity, int? PickupPointId, string? DeliveryAddress);
}
