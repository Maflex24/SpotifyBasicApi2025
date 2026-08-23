using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

/// <summary>Token Spotify zaszyfrowany w HttpOnly cookie konkretnej sesji przeglądarki.</summary>
public class TokenManagerService
{
    private const string CookieName = "spotify_library_session";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _userIdentityLoadLock = new(1, 1);

    public readonly record struct UserSessionSnapshot(long Version, string? CachedSpotifyUserId);

    private sealed class ProtectedSession
    {
        public SpotifyTokenResponse Token { get; set; } = new();
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public long Version { get; set; }
        public string? CachedSpotifyUserId { get; set; }
    }

    public TokenManagerService(IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _protector = dataProtectionProvider.CreateProtector("SpotifyBasicApi2025.TokenSession.v1");
    }

    public SpotifyTokenResponse? GetCurrentToken() => ReadSession()?.Token;

    public void SetToken(SpotifyTokenResponse token)
    {
        var previous = ReadSession();
        if (string.IsNullOrWhiteSpace(token.RefreshToken)) token.RefreshToken = previous?.Token.RefreshToken;
        WriteSession(new ProtectedSession
        {
            Token = token,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
            Version = (previous?.Version ?? 0) + 1
        });
    }

    public void ClearToken() => _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax });
    public bool HasValidToken() => !string.IsNullOrWhiteSpace(GetCurrentToken()?.AccessToken);
    public string? GetAccessToken() => GetCurrentToken()?.AccessToken;
    public string? GetRefreshToken() => GetCurrentToken()?.RefreshToken;

    public bool IsExpiringSoon(int seconds = 60)
    {
        var session = ReadSession();
        return session != null && DateTimeOffset.UtcNow.AddSeconds(seconds) >= session.ExpiresAtUtc;
    }

    public DateTimeOffset? GetExpiresAt() => ReadSession()?.ExpiresAtUtc;

    public UserSessionSnapshot? GetUserSessionSnapshot()
    {
        var session = ReadSession();
        return session == null || string.IsNullOrWhiteSpace(session.Token.AccessToken) ? null : new UserSessionSnapshot(session.Version, session.CachedSpotifyUserId);
    }

    public bool IsSessionCurrent(long version)
    {
        var session = ReadSession();
        return session != null && !string.IsNullOrWhiteSpace(session.Token.AccessToken) && session.Version == version;
    }

    public bool TryCacheSpotifyUserId(long version, string spotifyUserId)
    {
        var session = ReadSession();
        if (session == null || string.IsNullOrWhiteSpace(session.Token.AccessToken) || session.Version != version) return false;
        session.CachedSpotifyUserId = spotifyUserId;
        WriteSession(session);
        return true;
    }

    public Task WaitForUserIdentityLoadAsync() => _userIdentityLoadLock.WaitAsync();
    public void ReleaseUserIdentityLoad() => _userIdentityLoadLock.Release();

    private ProtectedSession? ReadSession()
    {
        var value = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonSerializer.Deserialize<ProtectedSession>(_protector.Unprotect(value)); }
        catch { return null; }
    }

    private void WriteSession(ProtectedSession session)
    {
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("Sesja Spotify wymaga żądania HTTP.");
        context.Response.Cookies.Append(CookieName, _protector.Protect(JsonSerializer.Serialize(session)), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true
        });
    }
}
