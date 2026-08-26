using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities;

/// <summary>
/// Small, durable textual description of a user-owned Spotify item. It is a fallback,
/// not a local mirror of the Spotify catalog.
/// </summary>
public sealed class LibItemSnapshot
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string ItemType { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string SpotifyItemId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string ArtistsJson { get; set; } = "[]";

    [MaxLength(500)]
    public string? AlbumTitle { get; set; }

    [MaxLength(500)]
    public string? OwnerName { get; set; }

    [MaxLength(512)]
    public string? SpotifyUri { get; set; }

    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastConfirmedAtUtc { get; set; } = DateTime.UtcNow;
}
