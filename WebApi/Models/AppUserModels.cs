namespace SpotifyBasicApi2025.Models;

public class AppUserCredentialsRequest
{
    public string Nick { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AppUserCreateRequest : AppUserCredentialsRequest
{
    public bool IsAdmin { get; set; }
}

public sealed class AppUserUpdateRequest
{
    public string Nick { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string? NewPassword { get; set; }
}

public sealed class AppUserModel
{
    public long Id { get; set; }
    public string Nick { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SystemSessionStatusModel
{
    public bool SetupRequired { get; set; }
    public AppUserSessionStatus AppUser { get; set; } = new();
    public SpotifySessionStatus Spotify { get; set; } = new();
}

public sealed class AppUserSessionStatus
{
    public bool IsLoggedIn { get; set; }
    public string? Nick { get; set; }
    public bool IsAdmin { get; set; }
}

public sealed class SpotifyAccountLinkModel
{
    public string SpotifyUserId { get; set; } = string.Empty;
    public string? SpotifyDisplayName { get; set; }
    public DateTime LinkedAtUtc { get; set; }
}

public sealed class SpotifySessionStatus
{
    public bool IsLoggedIn { get; set; }
    public string? DisplayName { get; set; }
    public string? Id { get; set; }
}
