using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Security.Claims;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class ReviewEndpoints
{
    public static void MapReviews(this WebApplication app)
    {
        app.MapGet("/api/products/{productId:int}/reviews", async (int productId,
            MarketplaceEntities db, CancellationToken ct) =>
        {
            if (!await db.Products.AnyAsync(product => product.Id == productId, ct))
                return Results.NotFound();
            var reviews = await db.Reviews.AsNoTracking().Where(review => review.ProductId == productId)
                .OrderBy(review => review.UserId)
                .Select(review => new { review.UserId, Login = review.Users!.Login, review.Rating, review.Comment })
                .ToListAsync(ct);
            return Results.Ok(new
            {
                Items = reviews,
                Rating = reviews.Count == 0 ? (double?)null : reviews.Average(review => review.Rating)
            });
        });

        app.MapPut("/api/products/{productId:int}/reviews", async (int productId, ReviewInput input,
            ClaimsPrincipal user, MarketplaceEntities db, CancellationToken ct) =>
        {
            if (input.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(input.Comment) || input.Comment.Length > 4000)
                return Results.BadRequest(new { error = "Укажите оценку от 1 до 5 и отзыв до 4000 символов." });
            if (!await db.Products.AnyAsync(product => product.Id == productId, ct))
                return Results.NotFound();
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var review = await db.Reviews.FindAsync(ct, userId, productId);
            if (review is null)
            {
                review = new Reviews { UserId = userId, ProductId = productId };
                db.Reviews.Add(review);
            }
            review.Rating = input.Rating;
            review.Comment = input.Comment.Trim();
            try
            {
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }
            catch (DbUpdateException error) when (error is DbUpdateConcurrencyException
                || error.GetBaseException() is SqlException { Number: 2601 or 2627 })
            {
                return Results.Conflict(new { error = "Отзывы изменились. Обновите список и повторите действие." });
            }
        }).RequireAuthorization();

        app.MapDelete("/api/admin/products/{productId:int}/reviews/{userId:int}",
            async (int productId, int userId, MarketplaceEntities db, CancellationToken ct) =>
        {
            var review = await db.Reviews.FindAsync(ct, userId, productId);
            if (review is not null)
            {
                db.Reviews.Remove(review);
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another administrator already removed the review.
                }
            }
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private sealed record ReviewInput(int Rating, string? Comment);
}
