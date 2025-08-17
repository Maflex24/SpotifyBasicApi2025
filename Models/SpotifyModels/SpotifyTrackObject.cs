using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyTrackObject
    {
        [JsonPropertyName("artists")]
        public List<SpotifyArtistObject> Artists { get; set; } = new();

        [JsonPropertyName("album")]
        public SpotifyAlbumObject? Album { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string> AvailableMarkets { get; set; } = new();

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("is_playable")]
        public bool? IsPlayable { get; set; }

        [JsonPropertyName("linked_from")]
        public SpotifyTrackObject? LinkedFrom { get; set; }

        [JsonPropertyName("restrictions")]
        public SpotifyRestrictions? Restrictions { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }
}
