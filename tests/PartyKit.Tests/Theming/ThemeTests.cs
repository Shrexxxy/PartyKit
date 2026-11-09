using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using PartyKit.Api.Theming;
using PartyKit.Tests.Infrastructure;

namespace PartyKit.Tests.Theming;

public class ThemeTests : IDisposable
{
    private const string Valid =
        """{ "title": "x", "host": { "name": "Аня", "birthDate": "2000-01-01" }, "partyDate": "2026-01-01", "currency": { "one": "a", "few": "a", "many": "b" } }""";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "party-theme-tests", Guid.NewGuid().ToString("N"));

    public ThemeTests() => Directory.CreateDirectory(_dir);

    [Fact]
    public async Task Api_returns_theme_without_dates()
    {
        using var factory = new AppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/theme");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var theme = JsonDocument.Parse(json).RootElement;
        theme.GetProperty("title").GetString().Should().Be("Тестовый праздник");
        theme.GetProperty("host").GetProperty("name").GetString().Should().Be("Анна");
        var currency = theme.GetProperty("currency");
        currency.GetProperty("one").GetString().Should().Be("монетка");
        currency.GetProperty("few").GetString().Should().Be("монетки");
        currency.GetProperty("many").GetString().Should().Be("монеток");
        currency.GetProperty("iconUrl").GetString().Should().Be("/theme/coin.svg");
        theme.GetProperty("palette").GetProperty("accent").GetString().Should().Be("#ff00aa");
        json.Should().NotContain("2001").And.NotContain("birthDate").And.NotContain("partyDate");
    }

    [Fact]
    public async Task Theme_assets_are_served()
    {
        using var factory = new AppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/theme/coin.svg");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("<svg");
    }

    [Fact]
    public void Example_theme_from_repo_loads()
    {
        var theme = ThemeLoader.Load(Path.Combine(AppContext.BaseDirectory, "theme"));

        theme.Title.Should().NotBeNullOrWhiteSpace();
        theme.Host.Name.Should().NotBeNullOrWhiteSpace();
        theme.Currency.One.Should().NotBeNullOrWhiteSpace();
        theme.AgeInDays.Should().BePositive();
    }

    [Fact]
    public void Missing_fields_are_named()
    {
        Write("""{ "title": "x", "host": { "birthDate": "2000-01-01" }, "partyDate": "2026-01-01", "currency": { "few": "a", "many": "b" } }""");

        var act = () => ThemeLoader.Load(_dir);

        act.Should().Throw<ThemeException>()
            .Which.Message.Should().Contain("currency.one").And.Contain("host.name");
    }

    [Fact]
    public void Broken_json_is_reported()
    {
        Write("{ title:");

        var act = () => ThemeLoader.Load(_dir);

        act.Should().Throw<ThemeException>().Which.Message.Should().Contain("битый JSON");
    }

    [Fact]
    public void Missing_icon_file_is_reported()
    {
        Write(Valid.Replace("\"many\": \"b\"", "\"many\": \"b\", \"icon\": \"nope.png\""));

        var act = () => ThemeLoader.Load(_dir);

        act.Should().Throw<ThemeException>().Which.Message.Should().Contain("currency.icon");
    }

    [Fact]
    public void Missing_file_is_reported()
    {
        var act = () => ThemeLoader.Load(Path.Combine(_dir, "nothing"));

        act.Should().Throw<ThemeException>().Which.Message.Should().Contain("theme.json");
    }

    [Fact]
    public void App_does_not_start_with_broken_theme()
    {
        Write(Valid.Replace("\"one\": \"a\", ", ""));
        using var factory = new AppFactory(themeDir: _dir);

        var act = () => factory.CreateClient();

        act.Should().Throw<Exception>().Where(e => e.ToString().Contains("currency.one"));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // Временную папку не жалко оставить.
        }
    }

    private void Write(string json) => File.WriteAllText(Path.Combine(_dir, "theme.json"), json);
}
