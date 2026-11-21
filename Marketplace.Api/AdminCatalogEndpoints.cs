using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class AdminCatalogEndpoints
{
    public static void MapAdminCatalog(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
        admin.MapGet("/products", async (MarketplaceEntities db, CancellationToken ct) =>
            await db.Products.AsNoTracking().OrderBy(product => product.Id)
                .Select(product => new
                {
                    product.Id, product.CategoryId, product.Name, product.Description, product.ImagePath,
                    product.PriceKopecks, product.Stock, product.PackSize, product.IsAvailable
                }).ToListAsync(ct));

        admin.MapPost("/products", (ProductInput input, MarketplaceEntities db, CancellationToken ct) =>
            SaveProductAsync(null, input, db, ct));
        admin.MapPut("/products/{id:int}", (int id, ProductInput input, MarketplaceEntities db, CancellationToken ct) =>
            SaveProductAsync(id, input, db, ct));
        admin.MapPost("/categories", (CategoryInput input, MarketplaceEntities db, CancellationToken ct) =>
            SaveCategoryAsync(null, input, db, ct));
        admin.MapPut("/categories/{id:int}", (int id, CategoryInput input, MarketplaceEntities db, CancellationToken ct) =>
            SaveCategoryAsync(id, input, db, ct));
    }

    private static async Task<IResult> SaveCategoryAsync(int? id, CategoryInput input, MarketplaceEntities db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 150)
            return Results.BadRequest(new { error = "Название категории должно содержать от 1 до 150 символов." });
        var category = id.HasValue ? await db.Categories.FindAsync(ct, id.Value) : new Categories();
        if (category is null)
            return Results.NotFound();
        category.Name = input.Name.Trim();
        if (!id.HasValue)
            db.Categories.Add(category);
        try
        {
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { category.Id, category.Name });
        }
        catch (DbUpdateException error) when (error is DbUpdateConcurrencyException
            || error.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Results.Conflict(new { error = "Категория уже существует или была изменена." });
        }
    }

    private static async Task<IResult> SaveProductAsync(int? id, ProductInput input, MarketplaceEntities db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 512
            || string.IsNullOrWhiteSpace(input.Description)
            || string.IsNullOrWhiteSpace(input.ImagePath) || input.ImagePath.Length > 512
            || input.ImagePath.Contains('/') || input.ImagePath.Contains('\\') || input.ImagePath.Contains(':')
            || input.PriceKopecks < 0 || input.Stock < 0 || input.PackSize is <= 0
            || input.IsAvailable is < 0 or > 1)
            return Results.BadRequest(new { error = "Проверьте поля товара. Для изображения укажите имя локального файла." });
        if (!await db.Categories.AnyAsync(category => category.Id == input.CategoryId, ct))
            return Results.BadRequest(new { error = "Выберите существующую категорию." });
        var product = id.HasValue ? await db.Products.FindAsync(ct, id.Value) : new Products();
        if (product is null)
            return Results.NotFound();
        product.Name = input.Name.Trim();
        product.CategoryId = input.CategoryId;
        product.Description = input.Description.Trim();
        product.ImagePath = input.ImagePath.Trim();
        product.PriceKopecks = input.PriceKopecks;
        product.Stock = input.Stock;
        product.PackSize = input.PackSize;
        product.IsAvailable = input.IsAvailable;
        if (!id.HasValue)
            db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { product.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "Товар изменился. Обновите список." });
        }
    }

    private sealed record CategoryInput(string? Name);
    private sealed record ProductInput(int CategoryId, string? Name, string? Description, string? ImagePath,
        int PriceKopecks, int Stock, int? PackSize, int IsAvailable);
}
