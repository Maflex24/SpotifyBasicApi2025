using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities;

/// <summary>Links a local application account to the stable Spotify owner id used by legacy library data.</summary>
public sealed class AppUserSpotifyLink
{
    [Key]
    public long AppUserId { get; set; }

    [Required, MaxLength(255)]
    public string SpotifyUserId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? SpotifyDisplayName { get; set; }

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public AppUser AppUser { get; set; } = null!;
}
