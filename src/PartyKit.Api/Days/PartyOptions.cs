namespace PartyKit.Api.Days;

/// <summary>
/// Настройки проведения из конфига (секция <c>Party</c>). Даты и имена праздника — в теме.
/// </summary>
public class PartyOptions
{
    /// <summary>Имя секции конфига.</summary>
    public const string Section = "Party";

    /// <summary>Отсчёт перед стартом ивента, секунды: экраны показывают «Приготовьтесь!»; 0 — старт сразу.</summary>
    public int StartDelaySeconds { get; set; } = 15;
}
