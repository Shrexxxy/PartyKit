namespace PartyKit.Api.Theming;

/// <summary>Именинник: имя на экранах и дата рождения для игры «Сколько мне дней».</summary>
public record ThemeHost(string Name, DateOnly BirthDate);

/// <summary>Валюта праздника: три формы слова и необязательная иконка.</summary>
/// <param name="One">1, 21, 101 — «монетка».</param>
/// <param name="Few">2–4, 22 — «монетки».</param>
/// <param name="Many">0, 5–20, 25 — «монеток».</param>
/// <param name="Icon">Файл в <c>assets/</c> темы; null — встроенная монета.</param>
public record ThemeCurrency(string One, string Few, string Many, string? Icon);

/// <summary>
/// Тема праздника из <c>theme.json</c>: название, именинник, даты, валюта, палитра.
/// </summary>
/// <param name="Dir">Папка темы.</param>
/// <param name="Palette">Имя CSS-переменной без <c>--</c> → значение.</param>
public record PartyTheme(
    string Dir,
    string Title,
    ThemeHost Host,
    DateOnly PartyDate,
    ThemeCurrency Currency,
    IReadOnlyDictionary<string, string> Palette)
{
    /// <summary>Папка картинок темы, раздаётся как <c>/theme/…</c>.</summary>
    public string AssetsDir => Path.Combine(Dir, "assets");

    /// <summary>Сколько дней имениннику в день праздника.</summary>
    public int AgeInDays => PartyDate.DayNumber - Host.BirthDate.DayNumber;
}

/// <summary>Тема не загрузилась: нет файла, битый JSON или не заполнены поля.</summary>
public class ThemeException(string message) : Exception(message);
