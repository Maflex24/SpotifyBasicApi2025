using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;

namespace SpotifyBasicApi2025.Services;

/// <summary>Resolves the local account to its linked Spotify library owner id.</summary>
public sealed class LibraryOwnerService(
    DatabaseContext context,
    AppUserAuthService appUserAuthService,
    UserService userService)
{
    public async Task<string> RequireOwnerSpotifyUserIdAsync()
    {
        var appUser = await appUserAuthService.GetCurrentUserAsync()
            ?? throw new InvalidOperationException("Zaloguj się na konto aplikacji.");
        var link = await context.AppUserSpotifyLinks.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AppUserId == appUser.Id);
        return link?.SpotifyUserId
            ?? throw new InvalidOperationException("Połącz konto aplikacji z kontem Spotify.");
    }

    public async Task<string> RequireCurrentSpotifyOwnerAsync()
    {
        var owner = await RequireOwnerSpotifyUserIdAsync();
        var currentSpotifyUser = await userService.GetCurrentUserIdAsync();
        if (!string.Equals(owner, currentSpotifyUser, StringComparison.Ordinal))
            throw new InvalidOperationException("Zalogowane konto Spotify nie jest połączone z bieżącym kontem aplikacji.");
        return owner;
    }

    public async Task<AppUserSpotifyLink> LinkCurrentSpotifyAccountAsync()
    {
        var appUser = await appUserAuthService.GetCurrentUserAsync()
            ?? throw new UnauthorizedAccessException("Zaloguj się na konto aplikacji.");
        var spotifyUserId = await userService.GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(spotifyUserId))
            throw new InvalidOperationException("Zaloguj się również do Spotify, aby połączyć konto.");

        var existingForSpotify = await context.AppUserSpotifyLinks
            .SingleOrDefaultAsync(x => x.SpotifyUserId == spotifyUserId);
        if (existingForSpotify != null && existingForSpotify.AppUserId != appUser.Id)
            throw new InvalidOperationException("To konto Spotify jest już połączone z innym kontem aplikacji.");

        var link = await context.AppUserSpotifyLinks
            .SingleOrDefaultAsync(x => x.AppUserId == appUser.Id);
        if (link == null)
        {
            link = new AppUserSpotifyLink { AppUserId = appUser.Id };
            context.AppUserSpotifyLinks.Add(link);
        }

        var profile = await userService.GetCurrentUserAsync();
        link.SpotifyUserId = spotifyUserId;
        link.SpotifyDisplayName = profile?.DisplayName;
        link.LinkedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return link;
    }
}
