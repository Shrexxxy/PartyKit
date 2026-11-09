using Microsoft.EntityFrameworkCore;
using PartyKit.Api.Auth;
using PartyKit.Api.Data;
using PartyKit.Api.Data.Entities;
using PartyKit.Api.Live;
using PartyKit.Api.Scoring;

namespace PartyKit.Api.Coins;

/// <summary>
/// Начисление монет и таблица лидеров.
/// </summary>
public class CoinService(AppDbContext db, TimeProvider time, ILiveNotifier live)
{
    /// <summary>
    /// Начисляет (или списывает при отрицательном) монеты игроку.
    /// </summary>
    public async Task AwardAsync(Guid userId, int amount, string reason, Guid? eventId = null)
    {
        Add(userId, amount, reason, eventId);
        await db.SaveChangesAsync();
        await live.BroadcastAsync();
    }

    /// <summary>
    /// Переводит места рейтинга в монеты (линейно) и начисляет. Нулевые награды не пишутся.
    /// </summary>
    public async Task AwardRankingAsync(
        Guid eventId,
        IReadOnlyList<Ranked<Guid>> ranking,
        int maxCoins,
        string reason)
    {
        if (ranking.Count == 0)
        {
            return;
        }

        var distinctPlaces = ranking.Max(r => r.Place);
        foreach (var (userId, place) in ranking)
        {
            var amount = PointsCalculator.ForPlace(place, distinctPlaces, maxCoins);
            if (amount != 0)
            {
                Add(userId, amount, reason, eventId);
            }
        }

        await db.SaveChangesAsync();
        await live.BroadcastAsync();
    }

    /// <summary>
    /// Таблица лидеров: все игроки (с нулём, если наград нет), плотные места по сумме.
    /// </summary>
    /// <param name="eventId">Только награды этого ивента; <c>null</c> — общая.</param>
    public async Task<IReadOnlyList<LeaderboardRowDto>> GetLeaderboardAsync(Guid? eventId = null)
    {
        var players = await db.Users.AsNoTracking().Where(u => u.Role == UserRole.Player).ToListAsync();
        var awards = db.CoinAwards.AsNoTracking();
        if (eventId is not null)
        {
            awards = awards.Where(a => a.EventId == eventId);
        }

        var sums = await awards
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.UserId, x => x.Total);

        var ranked = DenseRanking.Rank(players, p => sums.GetValueOrDefault(p.Id), descending: true);
        // Покупки на аукционе не меняют места — только «на руках» в общей таблице.
        var spent = eventId is null
            ? await db.AuctionSales.AsNoTracking().GroupBy(s => s.UserId).Select(g => new { UserId = g.Key, Sum = g.Sum(s => s.Price) })
                .ToDictionaryAsync(x => x.UserId, x => x.Sum)
            : [];

        return ranked
            .Select(r => new LeaderboardRowDto(
                r.Item.Id,
                r.Item.Login,
                r.Item.ToMe().AvatarUrl,
                sums.GetValueOrDefault(r.Item.Id),
                r.Place,
                sums.GetValueOrDefault(r.Item.Id) - spent.GetValueOrDefault(r.Item.Id)))
            .OrderBy(r => r.Place)
            .ThenBy(r => r.Login, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Add(Guid userId, int amount, string reason, Guid? eventId) =>
        db.CoinAwards.Add(new CoinAward
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            Amount = amount,
            Reason = reason,
            CreatedAt = time.GetUtcNow(),
        });
}
