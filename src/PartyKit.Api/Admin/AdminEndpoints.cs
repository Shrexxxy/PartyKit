using Microsoft.EntityFrameworkCore;
using PartyKit.Api.Auth;
using PartyKit.Api.Data;
using PartyKit.Api.Data.Entities;
using PartyKit.Api.Coins;

namespace PartyKit.Api.Admin;

/// <summary>
/// Служебные действия админа.
/// </summary>
public static class AdminEndpoints
{
    /// <summary>
    /// Маршруты <c>/api/admin</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AuthEndpoints.AdminPolicy);

        group.MapPost("/awards", async (ManualAwardRequest request, CoinService coins, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.BadRequest();
            }

            if (!await db.Users.AnyAsync(u => u.Id == request.UserId && u.Role == UserRole.Player))
            {
                return Results.NotFound();
            }

            await coins.AwardAsync(request.UserId, request.Amount, request.Reason.Trim());
            return Results.NoContent();
        });

        group.MapPost("/reset", async (ResetService reset) =>
        {
            await reset.ResetAsync();
            return Results.NoContent();
        });

        return app;
    }
}
