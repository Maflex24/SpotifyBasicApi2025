using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities;

public sealed class AiSettings
{
    [Key]
    public int Id { get; set; } = 1;
    public bool Enabled { get; set; }
    public string AccessMode { get; set; } = "allowlist"; // allowlist, all
    public string AllowedUserIds { get; set; } = string.Empty;
    public int DailyRequestLimit { get; set; } = 20;
    public string DefaultModel { get; set; } = "gpt-5.6-luna";
    public string? OpenAiApiKeyProtected { get; set; }
    public string? LastFmApiKeyProtected { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AiExternalCacheEntry
{
    [Key]
    public long Id { get; set; }
    [MaxLength(80)] public string Source { get; set; } = string.Empty;
    [MaxLength(512)] public string CacheKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AiUsageMetric
{
    [Key]
    public long Id { get; set; }
    [MaxLength(128)] public string UserId { get; set; } = string.Empty;
    [MaxLength(128)] public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    [MaxLength(24)] public string Outcome { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
