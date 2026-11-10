using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Marketplace.Api.Data;

namespace Marketplace.Api;

internal static class AuthEndpoints
{
    public static void MapAccounts(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (Registration input, MarketplaceEntities db,
            IPasswordHasher<Users> hasher, CancellationToken cancellationToken) =>
        {
            var login = input.Login?.Trim();
            if (login is not { Length: >= 3 and <= 100 } || login.Any(char.IsWhiteSpace)
                || input.Password is not { Length: >= 8 and <= 128 }
                || input.Password != input.ConfirmPassword)
                return Results.BadRequest(new { error = "Проверьте логин, длину пароля и его подтверждение." });

            var user = new Users { Login = login, Role = "User" };
            user.PasswordHash = hasher.HashPassword(user, input.Password);
            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException error) when (error.GetBaseException() is SqlException { Number: 2601 or 2627 })
            {
                return Results.Conflict(new { error = "Этот логин уже занят." });
            }

            return Results.StatusCode(StatusCodes.Status201Created);
        });

        app.MapPost("/api/auth/login", async (Credentials input, MarketplaceEntities db,
            IPasswordHasher<Users> hasher, CancellationToken cancellationToken) =>
        {
            var login = input.Login?.Trim();
            if (login is not { Length: >= 3 and <= 100 } || input.Password is not { Length: >= 8 and <= 128 })
                return Results.Unauthorized();

            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Login == login, cancellationToken);
            if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password)
                == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role)
            ], BearerTokenDefaults.AuthenticationScheme);
            return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: BearerTokenDefaults.AuthenticationScheme);
        });

        app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            Login = user.Identity!.Name,
            Role = user.FindFirstValue(ClaimTypes.Role)
        })).RequireAuthorization();
    }

    private sealed record Registration(string? Login, string? Password, string? ConfirmPassword);
    private sealed record Credentials(string? Login, string? Password);
}
