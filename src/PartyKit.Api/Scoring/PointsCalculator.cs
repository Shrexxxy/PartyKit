namespace PartyKit.Api.Scoring;

/// <summary>
/// Переводит место в количество осликов. Схема одна — линейная: последнее место тоже получает, шаг между местами одинаковый.
/// </summary>
public static class PointsCalculator
{
    /// <summary>
    /// Ослики за место.
    /// </summary>
    /// <param name="place">Место, начиная с 1.</param>
    /// <param name="distinctPlaces">Число различных мест в рейтинге (N).</param>
    /// <param name="maxCoins">Максимум осликов за ивент.</param>
    public static int ForPlace(int place, int distinctPlaces, int maxCoins)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(place, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(distinctPlaces, place);

        return (int)Math.Round(maxCoins * (double)(distinctPlaces - place + 1) / distinctPlaces, MidpointRounding.AwayFromZero);
    }
}
