using PartyKit.Api.Data.Entities;

namespace PartyKit.Api.Coins;

/// <summary>
/// Строка таблицы лидеров.
/// </summary>
/// <param name="Cash">«На руках»: монеты минус покупки на аукционе; места считаются по <paramref name="Coins"/>.</param>
public record LeaderboardRowDto(Guid UserId, string Login, string? AvatarUrl, int Coins, int Place, int Cash);

/// <summary>
/// Ивент для вкладок таблицы.
/// </summary>
public record EventDto(Guid Id, EventType Type, string Title, EventStatus Status, int MaxCoins);

/// <summary>
/// Ручное начисление от админа.
/// </summary>
public record ManualAwardRequest(Guid UserId, int Amount, string Reason);

/// <summary>
/// Причины начислений.
/// </summary>
public static class CoinReasons
{
    public const string StartCapital = "Стартовый капитал";
    public const string RegistrationSpeed = "Регистрация: скорость";
    public const string RegistrationPassword = "Регистрация: пароль";
    public const string Avatar = "Аватарка";
}
