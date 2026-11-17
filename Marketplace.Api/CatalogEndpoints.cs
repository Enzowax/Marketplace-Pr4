using System.Data.Entity;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class CatalogEndpoints
{
    public static void MapCatalog(this WebApplication app)
    {
        app.MapGet("/api/products", async (
            MarketplaceEntities db, string? search, int? categoryId,
            int? minPriceKopecks, int? maxPriceKopecks, CancellationToken cancellationToken) =>
        {
            if (categoryId is <= 0 || minPriceKopecks is < 0 || maxPriceKopecks is < 0
                || minPriceKopecks > maxPriceKopecks)
            {
                return Results.BadRequest(new { error = "Invalid catalog filters." });
            }

            IQueryable<Products> products = db.Products.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var name = search.Trim();
                products = products.Where(product => product.Name.Contains(name));
            }
            if (categoryId.HasValue)
                products = products.Where(product => product.CategoryId == categoryId.Value);
            if (minPriceKopecks.HasValue)
                products = products.Where(product => product.PriceKopecks >= minPriceKopecks.Value);
            if (maxPriceKopecks.HasValue)
                products = products.Where(product => product.PriceKopecks <= maxPriceKopecks.Value);

            return Results.Ok(await products.OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .Select(product => new
                {
                    product.Id,
                    product.Name,
                    product.Description,
                    product.CategoryId,
                    Category = product.Categories!.Name,
                    product.PriceKopecks,
                    product.ImagePath,
                    product.Stock,
                    product.PackSize,
                    CanBuy = product.IsAvailable == 1 && product.Stock > 0,
                    Rating = product.Reviews.Average(review => (double?)review.Rating)
                })
                .ToListAsync(cancellationToken));
        });
    }
}
