using PartyKit.Api.Scoring;

namespace PartyKit.Api.Tournament;

/// <summary>
/// Итог сыгранного матча.
/// </summary>
/// <param name="Loser">null — проход без игры.</param>
public record MatchResult(TournamentRound Round, Guid? Winner, Guid? Loser);

/// <summary>
/// Матч недоигранного турнира: кто в нём и сыгран ли.
/// </summary>
public record MatchProgress(TournamentRound Round, Guid? PlayerA, Guid? PlayerB, Guid? Winner, bool Done);

/// <summary>
/// Места турнира: финал → 1/2, матч за 3-е → 3/4, проигравшие 1/4 делят следующее место,
/// проигравшие отборочных — ещё одно (плотно). Матч-вызов на места не влияет.
/// </summary>
public static class TournamentRanking
{
    /// <summary>
    /// Места по результатам всех матчей.
    /// </summary>
    public static IReadOnlyList<Ranked<Guid>> Rank(IEnumerable<MatchResult> results)
    {
        var list = results.ToList();
        var final = list.Single(r => r.Round == TournamentRound.Final);
        var third = list.SingleOrDefault(r => r.Round == TournamentRound.ThirdPlace);

        // Группы по убыванию: каждая непустая группа — следующее место.
        var groups = new List<IEnumerable<Guid?>>
        {
            new[] { final.Winner },
            new[] { final.Loser },
            new[] { third?.Winner },
            new[] { third?.Loser },
            LosersOf(list, TournamentRound.Quarter),
            LosersOf(list, TournamentRound.Qualifier),
        };

        var ranking = new List<Ranked<Guid>>();
        var place = 0;
        foreach (var group in groups)
        {
            var players = group.OfType<Guid>().ToList();
            if (players.Count == 0)
            {
                continue;
            }

            place++;
            ranking.AddRange(players.Select(p => new Ranked<Guid>(p, place)));
        }

        return ranking;
    }

    /// <summary>
    /// Места досрочно завершённого турнира: кто дальше прошёл — выше; на том же этапе тот, кто ещё в игре, — выше выбывшего.
    /// Иначе места достались бы только выбывшим (у тех, кто ещё играет, нет ни победы в финале, ни поражения).
    /// </summary>
    public static IReadOnlyList<Ranked<Guid>> RankUnfinished(IEnumerable<MatchProgress> matches)
    {
        var list = matches.Where(m => m.Round != TournamentRound.Challenge).ToList();
        var players = list.SelectMany(m => new[] { m.PlayerA, m.PlayerB }).OfType<Guid>().Distinct();
        var scores = players.ToDictionary(p => p, p =>
        {
            var own = list.Where(m => m.PlayerA == p || m.PlayerB == p).ToList();
            var stage = own.Max(m => Stage(m.Round));
            var stillPlaying = own.Any(m => !m.Done) || own.All(m => m.Winner == p);
            return stage * 2 + (stillPlaying ? 1 : 0);
        });

        var levels = scores.Values.Distinct().OrderDescending().ToList();
        return scores.Select(kv => new Ranked<Guid>(kv.Key, levels.IndexOf(kv.Value) + 1)).ToList();
    }

    // Матч за 3-е место — тот же этап, что и полуфинал: туда попадают его проигравшие.
    private static int Stage(TournamentRound round) => round switch
    {
        TournamentRound.Qualifier => 0,
        TournamentRound.Quarter => 1,
        TournamentRound.Semi or TournamentRound.ThirdPlace => 2,
        _ => 3,
    };

    private static IEnumerable<Guid?> LosersOf(List<MatchResult> results, TournamentRound round) =>
        results.Where(r => r.Round == round).Select(r => r.Loser);
}
