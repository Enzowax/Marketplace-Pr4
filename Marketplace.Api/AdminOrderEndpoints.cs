using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class AdminOrderEndpoints
{
    public static void MapAdminOrders(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
        admin.MapGet("/pickup-points", async (MarketplaceEntities db, CancellationToken ct) =>
            await db.PickupPoints.AsNoTracking().OrderBy(point => point.Id)
                .Select(point => new { point.Id, point.Address, point.IsActive }).ToListAsync(ct));
        admin.MapPost("/pickup-points", (PickupInput input, MarketplaceEntities db, CancellationToken ct) =>
            SavePickupAsync(null, input, db, ct));
        admin.MapPut("/pickup-points/{id:int}", (int id, PickupInput input, MarketplaceEntities db, CancellationToken ct) =>
            SavePickupAsync(id, input, db, ct));

        admin.MapGet("/orders", async (MarketplaceEntities db, CancellationToken ct) =>
            await db.Orders.AsNoTracking().OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
                .Select(order => new
                {
                    order.Id, order.UserId, Login = order.Users!.Login, order.CreatedAt, order.Status, order.TotalKopecks,
                    order.PickupPointId, order.DeliveryAddress,
                    PickupAddress = order.PickupPoints == null ? null : order.PickupPoints.Address,
                    Items = order.OrderItems.Select(item => new
                    {
                        item.ProductId, Name = item.Products!.Name, item.Quantity, item.UnitPriceKopecks
                    })
                }).ToListAsync(ct));

        admin.MapPut("/orders/{id:int}/status", async (int id, StatusInput input, MarketplaceEntities db, CancellationToken ct) =>
        {
            if (input.Status is not ("Placed" or "Processing" or "Shipped" or "Ready" or "Received" or "Cancelled"))
                return Results.BadRequest(new { error = "Выберите допустимый статус заказа." });
            var order = await db.Orders.FindAsync(ct, id);
            if (order is null)
                return Results.NotFound();
            order.Status = input.Status;
            try
            {
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "Заказ изменился. Обновите список." });
            }
        });
    }

    private static async Task<IResult> SavePickupAsync(int? id, PickupInput input, MarketplaceEntities db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Address) || input.Address.Length > 512 || input.IsActive is < 0 or > 1)
            return Results.BadRequest(new { error = "Укажите адрес ПВЗ до 512 символов и корректную доступность." });
        var point = id.HasValue ? await db.PickupPoints.FindAsync(ct, id.Value) : new PickupPoints();
        if (point is null)
            return Results.NotFound();
        point.Address = input.Address.Trim();
        point.IsActive = input.IsActive;
        if (!id.HasValue)
            db.PickupPoints.Add(point);
        try
        {
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { point.Id, point.Address, point.IsActive });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "ПВЗ изменился. Обновите список." });
        }
    }

    private sealed record PickupInput(string? Address, int IsActive);
    private sealed record StatusInput(string? Status);
}
