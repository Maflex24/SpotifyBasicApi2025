using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

public sealed class AppUserAuthService(DatabaseContext context, IHttpContextAccessor httpContextAccessor)
{
    private const string CookieName = "app_user_session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public async Task<bool> IsSetupRequiredAsync() => !await context.AppUsers.AnyAsync();

    public async Task<AppUser> SetupAsync(AppUserCredentialsRequest request)
    {
        ValidateCredentials(request.Nick, request.Password);
        if (await context.AppUsers.AnyAsync()) throw new InvalidOperationException("Konfiguracja pierwszego administratora została już zakończona.");

        var user = new AppUser { Nick = NormalizeNick(request.Nick), IsAdmin = true };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        context.AppUsers.Add(user);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new InvalidOperationException("Konfiguracja pierwszego administratora została już zakończona."); }
        await CreateSessionAsync(user);
        return user;
    }

    public async Task<AppUser?> LoginAsync(AppUserCredentialsRequest request)
    {
        var nick = NormalizeNick(request.Nick);
        var user = await context.AppUsers.SingleOrDefaultAsync(x => x.Nick == nick);
        if (user == null) return null;
        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed) return null;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        await CreateSessionAsync(user);
        return user;
    }

    public async Task LogoutAsync()
    {
        var rawToken = GetCookieValue();
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            var hash = HashToken(rawToken);
            var session = await context.AppUserSessions.SingleOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAtUtc == null);
            if (session != null)
            {
                session.RevokedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName, CookieOptions());
    }

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(typeof(AppUser), out var cached) == true) return cached as AppUser;
        var rawToken = GetCookieValue();
        if (string.IsNullOrWhiteSpace(rawToken)) return null;

        var now = DateTime.UtcNow;
        var user = await context.AppUserSessions
            .Where(x => x.TokenHash == HashToken(rawToken) && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .Select(x => x.AppUser)
            .SingleOrDefaultAsync();
        if (user != null && httpContext != null) httpContext.Items[typeof(AppUser)] = user;
        return user;
    }

    public async Task<AppUser> RequireAdministratorAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null || !user.IsAdmin) throw new UnauthorizedAccessException("Ta operacja wymaga zalogowanego administratora systemu.");
        return user;
    }

    public async Task<List<AppUserModel>> GetUsersAsync()
    {
        await RequireAdministratorAsync();
        return await context.AppUsers.AsNoTracking().OrderBy(x => x.Nick).Select(x => ToModel(x)).ToListAsync();
    }

    public async Task<AppUserModel> CreateUserAsync(AppUserCreateRequest request)
    {
        await RequireAdministratorAsync();
        ValidateCredentials(request.Nick, request.Password);
        var user = new AppUser { Nick = NormalizeNick(request.Nick), IsAdmin = request.IsAdmin };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        context.AppUsers.Add(user);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new ArgumentException("Konto o takim nicku już istnieje."); }
        return ToModel(user);
    }

    public async Task<AppUserModel> UpdateUserAsync(long id, AppUserUpdateRequest request)
    {
        var currentAdmin = await RequireAdministratorAsync();
        if (string.IsNullOrWhiteSpace(request.Nick)) throw new ArgumentException("Nick jest wymagany.");
        var user = await context.AppUsers.SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Nie znaleziono użytkownika.");
        if (user.Id == currentAdmin.Id && !request.IsAdmin) throw new ArgumentException("Nie możesz odebrać sobie uprawnień administratora.");
        if (user.IsAdmin && !request.IsAdmin && await context.AppUsers.CountAsync(x => x.IsAdmin) <= 1)
            throw new ArgumentException("Musi pozostać co najmniej jeden administrator.");
        if (!string.IsNullOrWhiteSpace(request.NewPassword)) ValidatePassword(request.NewPassword);

        user.Nick = NormalizeNick(request.Nick);
        user.IsAdmin = request.IsAdmin;
        if (!string.IsNullOrWhiteSpace(request.NewPassword)) user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new ArgumentException("Konto o takim nicku już istnieje."); }
        return ToModel(user);
    }

    public async Task DeleteUserAsync(long id)
    {
        var currentAdmin = await RequireAdministratorAsync();
        var user = await context.AppUsers.SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Nie znaleziono użytkownika.");
        if (user.Id == currentAdmin.Id) throw new ArgumentException("Nie możesz usunąć aktualnie zalogowanego konta.");
        if (user.IsAdmin && await context.AppUsers.CountAsync(x => x.IsAdmin) <= 1) throw new ArgumentException("Nie można usunąć ostatniego administratora.");
        context.AppUsers.Remove(user);
        await context.SaveChangesAsync();
    }

    private async Task CreateSessionAsync(AppUser user)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        context.AppUserSessions.Add(new AppUserSession { AppUserId = user.Id, TokenHash = HashToken(rawToken), ExpiresAtUtc = DateTime.UtcNow.Add(SessionLifetime) });
        await context.SaveChangesAsync();
        httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieName, rawToken, CookieOptions());
    }

    private string? GetCookieValue() => httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = httpContextAccessor.HttpContext?.Request.IsHttps ?? true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.Add(SessionLifetime),
        IsEssential = true
    };
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    private static string NormalizeNick(string nick) => nick.Trim();
    private static AppUserModel ToModel(AppUser user) => new() { Id = user.Id, Nick = user.Nick, IsAdmin = user.IsAdmin, CreatedAtUtc = user.CreatedAtUtc };
    private static void ValidateCredentials(string nick, string password) { if (string.IsNullOrWhiteSpace(nick) || nick.Trim().Length is < 3 or > 64) throw new ArgumentException("Nick musi mieć 3–64 znaki."); ValidatePassword(password); }
    private static void ValidatePassword(string password) { if (password.Length is < 12 or > 128) throw new ArgumentException("Hasło musi mieć 12–128 znaków."); }
}
