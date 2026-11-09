using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using PartyKit.Api.Auth;
using PartyKit.Api.Data;
using PartyKit.Api.Data.Entities;
using PartyKit.Api.Coins;
using PartyKit.Api.Live;
using PartyKit.Api.Registration;

namespace PartyKit.Api.Media;

/// <summary>
/// Селфи-аватарка и раздача медиафайлов.
/// </summary>
public static class AvatarEndpoints
{
    /// <summary>Максимальный размер загружаемого файла.</summary>
    public const long MaxUploadBytes = 100 * 1024 * 1024;

    /// <summary>Монеты за первую аватарку.</summary>
    public const int AvatarCoins = 5;

    /// <summary>
    /// Маршруты <c>/api/me/avatar</c> и <c>/media/{id}</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapMedia(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/me/avatar", async (
                IFormFile? file,
                ClaimsPrincipal principal,
                AppDbContext db,
                MediaStorage storage,
                RegistrationService registration,
                CoinService coins,
                ILiveNotifier live) =>
            {
                if ((await registration.GetStatusAsync()).State != RegistrationState.Finished)
                {
                    return Results.Conflict(new { error = "not_yet" });
                }

                if (file is null || file.Length == 0 || file.Length > MaxUploadBytes
                    || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "bad_file" });
                }

                var user = await db.Users.SingleAsync(u => u.Id == principal.UserId());
                await using var stream = file.OpenReadStream();
                var media = await storage.SaveAsync(stream, file.ContentType, MediaKind.Photo, user.Id, userId: user.Id);
                user.AvatarMediaId = media.Id;
                await db.SaveChangesAsync();

                // Награда — по факту прошлых начислений, а не по наличию аватарки: так не задвоится.
                var alreadyAwarded = await db.CoinAwards.AnyAsync(a => a.UserId == user.Id && a.Reason == CoinReasons.Avatar);
                if (alreadyAwarded)
                {
                    await live.BroadcastAsync();
                }
                else
                {
                    await coins.AwardAsync(user.Id, AvatarCoins, CoinReasons.Avatar);
                }

                return Results.Ok(user.ToMe());
            })
            .RequireAuthorization(AuthEndpoints.PlayerPolicy)
            .DisableAntiforgery();

        app.MapGet("/media/{id:guid}", async (Guid id, AppDbContext db, MediaStorage storage, HttpContext http) =>
        {
            var media = await db.MediaFiles.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);
            var path = media is null ? null : storage.PathFor(media);
            if (path is null || !File.Exists(path))
            {
                return Results.NotFound();
            }

            http.Response.Headers[HeaderNames.CacheControl] = "public, max-age=86400";
            // Диапазоны — чтобы видео и аудио перематывались и «Повторить» не качало файл заново.
            return Results.File(Path.GetFullPath(path), media!.ContentType, enableRangeProcessing: true);
        }).RequireAuthorization();

        return app;
    }
}
