using System.Net.Http.Json;
using AwesomeAssertions;
using PartyKit.Api.Events;
using PartyKit.Tests.Infrastructure;

namespace PartyKit.Tests.Events;

public class DefaultsTests : IDisposable
{
    private readonly AppFactory _factory = new();

    [Theory]
    [InlineData("Reaction", 30)]
    [InlineData("Tapper", 30)]
    [InlineData("Days", 30)]
    [InlineData("NeuroPhoto", 0)]
    public async Task Defaults_for_layer3_types(string type, int maxCoins)
    {
        var admin = await TestClients.AdminAsync(_factory);

        var created = await admin.PostAsJsonAsync("/api/admin/events", new { type, title = type }, TestClients.Json);

        (await created.Content.ReadFromJsonAsync<ProgramItemDto>(TestClients.Json))!.MaxCoins.Should().Be(maxCoins);
    }

    public void Dispose() => _factory.Dispose();
}
