using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyPlaylistTrackItem
    {
        [JsonPropertyName("added_at")]
        public string AddedAt { get; set; } = string.Empty;

        [JsonPropertyName("added_by")]
        public SpotifyUserProfile? AddedBy { get; set; }

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }

        [JsonPropertyName("track")]
        public SpotifyTrackObject? Track { get; set; }
    }
}
