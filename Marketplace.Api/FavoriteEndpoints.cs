using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Security.Claims;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class FavoriteEndpoints
{
    public static void MapFavorites(this WebApplication app)
    {
        var favorites = app.MapGroup("/api/favorites").RequireAuthorization();
        favorites.MapGet("", async (ClaimsPrincipal principal, MarketplaceEntities db, CancellationToken ct) =>
        {
            var userId = UserId(principal);
            return await db.Products.AsNoTracking().Where(product => product.Users.Any(user => user.Id == userId))
                .OrderBy(product => product.Name).ThenBy(product => product.Id)
                .Select(product => new
                {
                    product.Id, product.Name, product.Description, product.ImagePath,
                    product.PriceKopecks, product.Stock,
                    CanBuy = product.IsAvailable == 1 && product.Stock > 0
                }).ToListAsync(ct);
        });
        favorites.MapPut("/{productId:int}", (int productId, ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
            ChangeAsync(productId, user, db, remove: false, toCart: false, ct));
        favorites.MapDelete("/{productId:int}", (int productId, ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
            ChangeAsync(productId, user, db, remove: true, toCart: false, ct));
        favorites.MapPost("/{productId:int}/cart", (int productId, ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
            ChangeAsync(productId, user, db, remove: true, toCart: true, ct));
    }

    private static async Task<IResult> ChangeAsync(int productId, ClaimsPrincipal principal,
        MarketplaceEntities db, bool remove, bool toCart, CancellationToken ct)
    {
        using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var userId = UserId(principal);
            var user = await db.Users.Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == userId, ct);
            if (user is null)
                return Results.Unauthorized();
            var favorite = user.Products.SingleOrDefault(item => item.Id == productId);
            if (toCart)
            {
                if (favorite is null)
                    return Results.NotFound(new { error = "Товар уже удалён из избранного." });
                var cartItem = await db.CartItems.FindAsync(ct, userId, productId);
                var quantity = (long)(cartItem?.Quantity ?? 0) + 1;
                if (favorite.IsAvailable != 1 || quantity > favorite.Stock)
                    return Results.Conflict(new { error = "Товар недоступен в нужном количестве." });
                if (cartItem is null)
                    db.CartItems.Add(new CartItems { UserId = userId, ProductId = productId, Quantity = 1 });
                else
                    cartItem.Quantity = (int)quantity;
            }
            if (remove)
            {
                if (favorite is not null)
                    user.Products.Remove(favorite);
            }
            else if (favorite is null)
            {
                var product = await db.Products.SingleOrDefaultAsync(item => item.Id == productId, ct);
                if (product is null)
                    return Results.NotFound(new { error = "Товар не найден." });
                user.Products.Add(product);
            }
            await db.SaveChangesAsync(ct);
            transaction.Commit();
            return Results.NoContent();
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException
            || error is SqlException { Number: 1205 }
            || error is DbUpdateException && error.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 })
        {
            return Results.Conflict(new { error = "Данные изменились. Обновите список и повторите действие." });
        }
    }

    private static int UserId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
