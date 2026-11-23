using System.Data.Entity;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class AdminSetup
{
    public static async Task PromoteAsync(MarketplaceEntities db, string login)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Login == login);
        if (user is null)
            throw new InvalidOperationException("Сначала зарегистрируйте указанный аккаунт через приложение.");
        user.Role = "Admin";
        await db.SaveChangesAsync();
    }
}
