using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities;

public sealed class AppUser
{
    [Key]
    public long Id { get; set; }
    [MaxLength(64)] public string Nick { get; set; } = string.Empty;
    [MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AppUserSession> Sessions { get; set; } = new List<AppUserSession>();
}

public sealed class AppUserSession
{
    [Key]
    public long Id { get; set; }
    public long AppUserId { get; set; }
    [MaxLength(64)] public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public AppUser AppUser { get; set; } = null!;
}
