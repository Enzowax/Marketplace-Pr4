using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Security.Claims;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class CartEndpoints
{
    public static void MapCart(this WebApplication app)
    {
        var cart = app.MapGroup("/api/cart").RequireAuthorization();

        cart.MapGet("", async (ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
        {
            var userId = UserId(user);
            var items = await db.CartItems.AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderBy(item => item.Products!.Name)
                .Select(item => new
                {
                    item.ProductId,
                    item.Products!.Name,
                    item.Quantity,
                    item.Products.PriceKopecks,
                    CanBuy = item.Products.IsAvailable == 1 && item.Products.Stock >= item.Quantity
                }).ToListAsync(ct);
            return Results.Ok(new
            {
                Items = items,
                TotalKopecks = items.Sum(item => (decimal)item.PriceKopecks * item.Quantity)
            });
        });

        cart.MapPut("/{productId:int}", async (int productId, QuantityInput input, ClaimsPrincipal user,
            MarketplaceEntities db, CancellationToken ct) =>
        {
            if (input.Quantity <= 0)
                return Results.BadRequest(new { error = "Количество должно быть больше нуля." });

            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == productId, ct);
            if (product is null)
                return Results.NotFound(new { error = "Товар не найден." });
            if (product.IsAvailable != 1 || product.Stock < input.Quantity)
                return Results.Conflict(new { error = "Товар недоступен в указанном количестве." });

            var userId = UserId(user);
            var item = await db.CartItems.FindAsync(ct, userId, productId);
            if (item is null)
                db.CartItems.Add(new CartItems { UserId = userId, ProductId = productId, Quantity = input.Quantity });
            else
                item.Quantity = input.Quantity;

            try
            {
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }
            catch (DbUpdateException error) when (error is DbUpdateConcurrencyException
                || error.GetBaseException() is SqlException { Number: 2601 or 2627 })
            {
                return Results.Conflict(new { error = "Корзина изменилась. Обновите её и повторите действие." });
            }
        });

        cart.MapDelete("/{productId:int}", async (int productId, ClaimsPrincipal user,
            MarketplaceEntities db, CancellationToken ct) =>
        {
            var item = await db.CartItems.FindAsync(ct, UserId(user), productId);
            if (item is null)
                return Results.NoContent();
            db.CartItems.Remove(item);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The item has already been removed.
            }
            return Results.NoContent();
        });
    }

    private static int UserId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private sealed record QuantityInput(int Quantity);
}
