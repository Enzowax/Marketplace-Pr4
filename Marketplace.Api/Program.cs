using Marketplace.Api;
using System.Data.Entity;
using Marketplace.Api.Data;

DbConfiguration.SetConfiguration(new MarketplaceEfConfiguration());

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Marketplace")
    ?? throw new InvalidOperationException("Connection string 'Marketplace' is required.");

builder.Services.AddScoped(_ => new MarketplaceEntities(connectionString));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/categories", async (MarketplaceEntities db, CancellationToken cancellationToken) =>
    await db.Categories.AsNoTracking()
        .OrderBy(category => category.Name)
        .Select(category => new { category.Id, category.Name })
        .ToListAsync(cancellationToken));

app.MapCatalog();

app.Run();
