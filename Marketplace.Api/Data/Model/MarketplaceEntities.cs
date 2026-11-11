using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace Marketplace.Api.Data;

[DbConfigurationType(typeof(MarketplaceEfConfiguration))]
public sealed class MarketplaceEntities : DbContext
{
    public MarketplaceEntities(string sqlServerConnection)
        : base(new EntityConnection(new EntityConnectionStringBuilder
        {
            Metadata = "res://*/Data.Model.Marketplace.csdl|res://*/Data.Model.Marketplace.ssdl|res://*/Data.Model.Marketplace.msl",
            Provider = "System.Data.SqlClient",
            ProviderConnectionString = sqlServerConnection
        }.ConnectionString), contextOwnsConnection: true)
    {
        Configuration.LazyLoadingEnabled = false;
    }

    public DbSet<Categories> Categories => Set<Categories>();
    public DbSet<Products> Products => Set<Products>();
    public DbSet<Users> Users => Set<Users>();
    public DbSet<CartItems> CartItems => Set<CartItems>();
    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderItems> OrderItems => Set<OrderItems>();
    public DbSet<PickupPoints> PickupPoints => Set<PickupPoints>();
    public DbSet<Reviews> Reviews => Set<Reviews>();
}

public sealed class MarketplaceEfConfiguration : DbConfiguration
{
    public MarketplaceEfConfiguration()
    {
        SetProviderFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
}
