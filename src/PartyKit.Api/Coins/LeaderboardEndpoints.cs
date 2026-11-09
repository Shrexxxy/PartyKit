using Microsoft.EntityFrameworkCore;
using PartyKit.Api.Data;
using PartyKit.Api.Data.Entities;

namespace PartyKit.Api.Coins;

/// <summary>
/// Таблица лидеров и список ивентов для вкладок.
/// </summary>
public static class LeaderboardEndpoints
{
    /// <summary>
    /// Маршруты <c>/api/leaderboard</c> и <c>/api/events</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapLeaderboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/leaderboard", (Guid? eventId, CoinService coins) => coins.GetLeaderboardAsync(eventId))
            .RequireAuthorization();

        app.MapGet("/api/events", async (AppDbContext db) =>
        {
            var events = await db.Events.AsNoTracking()
                .Where(e => e.Status == EventStatus.Active || e.Status == EventStatus.Finished)
                // Вкладка нужна только тем, кто даёт монеты.
                .Where(e => e.Type != EventType.Ceremony)
                .ToListAsync();

            return events
                .OrderBy(e => e.StartedAt)
                .Select(e => new EventDto(e.Id, e.Type, e.Title, e.Status, e.MaxCoins));
        }).RequireAuthorization();

        return app;
    }
}
