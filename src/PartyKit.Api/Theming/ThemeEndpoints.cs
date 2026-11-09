using Microsoft.Extensions.FileProviders;

namespace PartyKit.Api.Theming;

/// <summary>Именинник для фронта — без даты рождения.</summary>
public record ThemeHostDto(string Name);

/// <summary>Валюта для фронта; <paramref name="IconUrl"/> null — встроенная монета.</summary>
public record ThemeCurrencyDto(string One, string Few, string Many, string? IconUrl);

/// <summary>Тема для фронта. Дат нет: по ним считается ответ игры «Сколько мне дней».</summary>
public record ThemeDto(string Title, ThemeHostDto Host, ThemeCurrencyDto Currency, IReadOnlyDictionary<string, string> Palette);

/// <summary>Тема: загрузка при старте, <c>GET /api/theme</c>, картинки темы как <c>/theme/…</c>.</summary>
public static class ThemeEndpoints
{
    /// <summary>Адрес картинок темы.</summary>
    public const string AssetsPath = "/theme";

    /// <summary>
    /// Регистрирует тему из <c>Theme:Dir</c> (по умолчанию <c>theme/</c> рядом с приложением).
    /// Относительный путь — от папки приложения.
    /// </summary>
    public static IServiceCollection AddPartyTheme(this IServiceCollection services, IConfiguration config) =>
        services.AddSingleton(_ => ThemeLoader.Load(Path.GetFullPath(config["Theme:Dir"] ?? "theme", AppContext.BaseDirectory)));

    /// <summary>Читает тему сразу — битая тема роняет запуск, а не первый запрос посреди праздника.</summary>
    public static WebApplication UsePartyTheme(this WebApplication app)
    {
        var theme = app.Services.GetRequiredService<PartyTheme>();
        if (Directory.Exists(theme.AssetsDir))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(theme.AssetsDir),
                RequestPath = AssetsPath,
            });
        }

        return app;
    }

    /// <summary>Отдаёт тему фронту, без входа: она нужна уже экрану регистрации.</summary>
    public static void MapTheme(this WebApplication app) =>
        app.MapGet("/api/theme", (PartyTheme theme) => new ThemeDto(
            theme.Title,
            new ThemeHostDto(theme.Host.Name),
            new ThemeCurrencyDto(
                theme.Currency.One,
                theme.Currency.Few,
                theme.Currency.Many,
                theme.Currency.Icon is null ? null : $"{AssetsPath}/{string.Join('/', theme.Currency.Icon.Split('/').Select(Uri.EscapeDataString))}"),
            theme.Palette));
}
