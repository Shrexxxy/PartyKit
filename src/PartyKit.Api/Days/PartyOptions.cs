namespace PartyKit.Api.Days;

/// <summary>
/// Даты праздника из конфига (секция <c>Party</c>).
/// </summary>
public class PartyOptions
{
    /// <summary>Имя секции конфига.</summary>
    public const string Section = "Party";

    /// <summary>День рождения именинника.</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>День праздника.</summary>
    public DateOnly PartyDate { get; set; }

    /// <summary>Отсчёт перед стартом ивента, секунды: экраны показывают «Приготовьтесь!»; 0 — старт сразу.</summary>
    public int StartDelaySeconds { get; set; } = 15;

    /// <summary>Сколько дней имениннику в день праздника.</summary>
    public int AgeInDays => PartyDate.DayNumber - BirthDate.DayNumber;
}
