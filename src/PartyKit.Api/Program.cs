using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PartyKit.Api.Admin;
using PartyKit.Api.Auction;
using PartyKit.Api.Auth;
using PartyKit.Api.Bosses;
using PartyKit.Api.Ceremony;
using PartyKit.Api.Data;
using PartyKit.Api.Data.Entities;
using PartyKit.Api.Days;
using PartyKit.Api.Coins;
using PartyKit.Api.Events;
using PartyKit.Api.HideAndSeek;
using PartyKit.Api.Laugh;
using PartyKit.Api.Live;
using PartyKit.Api.Mafia;
using PartyKit.Api.Media;
using PartyKit.Api.Neuro;
using PartyKit.Api.Pets;
using PartyKit.Api.PhotoHunt;
using PartyKit.Api.Quiz;
using PartyKit.Api.Reaction;
using PartyKit.Api.Registration;
using PartyKit.Api.Spa;
using PartyKit.Api.Tapper;
using PartyKit.Api.Theming;
using PartyKit.Api.Tote;
using PartyKit.Api.Tournament;
using PartyKit.Api.Tv;

var builder = WebApplication.CreateBuilder(args);

// Журнал событий Windows без прав администратора отказывает в записи — и падает запрос, который что-то логирует.
builder.Logging.ClearProviders().AddConsole().AddDebug();

var tempPath = builder.Configuration["ASPNETCORE_TEMP"]
               ?? Path.Combine(AppContext.BaseDirectory, "temp");

Directory.CreateDirectory(tempPath);

Environment.SetEnvironmentVariable("ASPNETCORE_TEMP", tempPath);

Console.WriteLine($"ASP.NET Core TEMP: {tempPath}");

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.Section));
builder.Services.Configure<AccountsOptions>(builder.Configuration.GetSection(AccountsOptions.Section));
builder.Services.Configure<PartyOptions>(builder.Configuration.GetSection(PartyOptions.Section));
builder.Services.AddPartyTheme(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var storage = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    Directory.CreateDirectory(storage.DataDir);
    options.UseSqlite($"Data Source={storage.DatabasePath}");
});
builder.Services.AddScoped<StaffSeeder>();
builder.Services.AddScoped<CoinService>();
builder.Services.AddSingleton<ILiveNotifier, LiveNotifier>();
builder.Services.AddScoped<SnapshotBuilder>();
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddSingleton<IPasswordAnalyzer, ZxcvbnPasswordAnalyzer>();
builder.Services.AddSingleton<IPasswordHasher<RegistrationAttempt>, PasswordHasher<RegistrationAttempt>>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<MediaStorage>();
builder.Services.AddScoped<TvFactsService>();
builder.Services.AddScoped<ResetService>();
builder.Services.AddScoped<EventEngine>();
builder.Services.AddHostedService<EventTicker>();
builder.Services.AddSingleton(Random.Shared);
builder.Services.AddScoped<IEventModule, TournamentModule>();
builder.Services.AddScoped<TournamentService>();
builder.Services.AddScoped<ToteService>();
builder.Services.AddScoped<LaughModule>();
builder.Services.AddScoped<IEventModule>(sp => sp.GetRequiredService<LaughModule>());
builder.Services.AddScoped<HideSeekModule>();
builder.Services.AddScoped<IEventModule>(sp => sp.GetRequiredService<HideSeekModule>());
builder.Services.AddScoped<HideSeekService>();
builder.Services.AddScoped<IEventModule, PhotoHuntModule>();
builder.Services.AddScoped<PhotoHuntService>();
builder.Services.AddScoped<TvContentService>();
builder.Services.AddScoped<TvSlidesService>();
builder.Services.AddScoped<CeremonyModule>();
builder.Services.AddScoped<IEventModule>(sp => sp.GetRequiredService<CeremonyModule>());
builder.Services.AddScoped<IEventModule, ReactionModule>();
builder.Services.AddScoped<ReactionService>();
builder.Services.AddScoped<IEventModule, TapperModule>();
builder.Services.AddScoped<TapperService>();
builder.Services.AddScoped<IEventModule, DaysModule>();
builder.Services.AddScoped<DaysService>();
builder.Services.AddScoped<IEventModule, NeuroModule>();
builder.Services.AddScoped<NeuroService>();
builder.Services.AddSingleton(_ => NeuroCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Neuro", "neuro-rounds.json")));
builder.Services.AddScoped<QuizContentService>();
builder.Services.AddScoped<IEventModule, QuizModule>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddSingleton<TvPreloadStatus>();
builder.Services.AddScoped<AuctionLotService>();
builder.Services.AddScoped<IEventModule, AuctionModule>();
builder.Services.AddScoped<AuctionService>();
builder.Services.AddScoped<IEventModule, MafiaModule>();
builder.Services.AddScoped<MafiaService>();
builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<IEventModule, BossModule>();
builder.Services.AddScoped<BossService>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddPartyAuth();
builder.Services.AddPartyProxy();
builder.Services.AddDataProtection()
    .SetApplicationName("PartyKit")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration.GetSection(StorageOptions.Section).Get<StorageOptions>()?.KeysDir ?? new StorageOptions().KeysDir));

var app = builder.Build();

app.UsePartyTheme();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<StaffSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<TvContentService>().SeedCaptionsAsync();
}

app.UseForwardedHeaders();

// Файлы (видео Своей игры) — до 100 МБ; остальные запросы — со стандартным лимитом сервера (~30 МБ).
app.Use((context, next) =>
{
    if (context.Request.HasFormContentType && context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
    {
        limit.MaxRequestBodySize = AvatarEndpoints.MaxUploadBytes + 1024 * 1024;
    }

    return next(context);
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapTheme();
app.MapAuth();
app.MapLeaderboard();
app.MapAdmin();
app.MapRegistration();
app.MapMedia();
app.MapTv();
app.MapEvents();
app.MapTournament();
app.MapTote();
app.MapLaugh();
app.MapHideSeek();
app.MapPhotoHunt();
app.MapCeremony();
app.MapReaction();
app.MapTapper();
app.MapDays();
app.MapNeuro();
app.MapQuizContent();
app.MapQuiz();
app.MapQuizPreload();
app.MapAuctionLots();
app.MapAuction();
app.MapMafia();
app.MapPets();
app.MapBoss();
app.MapHub<LiveHub>("/hub");
app.MapSpa();

app.Run();

/// <summary>
/// Точка входа; partial — для тестового хоста.
/// </summary>
public partial class Program;
