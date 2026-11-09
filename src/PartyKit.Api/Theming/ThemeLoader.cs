using System.Text.Json;
using System.Text.RegularExpressions;

namespace PartyKit.Api.Theming;

/// <summary>
/// Читает и проверяет <c>theme.json</c>. Ошибки собираются все сразу — чтобы чинить тему за один заход, а не по полю.
/// </summary>
public static partial class ThemeLoader
{
    /// <summary>Имя файла темы в её папке.</summary>
    public const string FileName = "theme.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Загружает тему из папки; не вышло — <see cref="ThemeException"/> с перечнем полей.</summary>
    public static PartyTheme Load(string dir)
    {
        var path = Path.Combine(dir, FileName);
        if (!File.Exists(path))
        {
            throw new ThemeException($"Тема: нет файла {path}");
        }

        RawTheme? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawTheme>(File.ReadAllText(path), Json);
        }
        catch (JsonException e)
        {
            throw new ThemeException($"Тема {path}: битый JSON — {e.Message}");
        }

        raw ??= new RawTheme();
        var errors = new List<string>();
        var title = Required(raw.Title, "title", errors);
        var hostName = Required(raw.Host?.Name, "host.name", errors);
        var birthDate = Required(raw.Host?.BirthDate, "host.birthDate", errors);
        var partyDate = Required(raw.PartyDate, "partyDate", errors);
        var one = Required(raw.Currency?.One, "currency.one", errors);
        var few = Required(raw.Currency?.Few, "currency.few", errors);
        var many = Required(raw.Currency?.Many, "currency.many", errors);

        var icon = string.IsNullOrWhiteSpace(raw.Currency?.Icon) ? null : raw.Currency.Icon;
        if (icon is not null && !File.Exists(Path.Combine(dir, "assets", icon)))
        {
            errors.Add($"currency.icon: нет файла assets/{icon}");
        }

        if (birthDate is { } born && partyDate is { } party && party <= born)
        {
            errors.Add("partyDate: праздник раньше дня рождения");
        }

        var palette = raw.Palette ?? [];
        errors.AddRange(palette.Keys.Where(k => !CssName().IsMatch(k)).Select(k => $"palette.{k}: имя CSS-переменной — латиница, цифры и «-», без «--»"));

        if (errors.Count > 0)
        {
            throw new ThemeException($"Тема {path}: {string.Join("; ", errors.Select(e => e.Contains(':') ? e : $"нет поля {e}"))}");
        }

        return new PartyTheme(dir, title!, new ThemeHost(hostName!, birthDate!.Value), partyDate!.Value, new ThemeCurrency(one!, few!, many!, icon), palette);
    }

    private static string? Required(string? value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(field);
        }

        return value;
    }

    private static DateOnly? Required(DateOnly? value, string field, List<string> errors)
    {
        if (value is null)
        {
            errors.Add(field);
        }

        return value;
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex CssName();

    #region Сырой JSON: все поля необязательные, проверяем сами

    private sealed record RawTheme(string? Title = null, RawHost? Host = null, DateOnly? PartyDate = null, RawCurrency? Currency = null, Dictionary<string, string>? Palette = null);

    private sealed record RawHost(string? Name = null, DateOnly? BirthDate = null);

    private sealed record RawCurrency(string? One = null, string? Few = null, string? Many = null, string? Icon = null);

    #endregion
}
