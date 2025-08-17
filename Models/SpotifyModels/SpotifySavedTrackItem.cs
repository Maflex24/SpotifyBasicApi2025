using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifySavedTrackItem
    {
        [JsonPropertyName("added_at")]
        public string AddedAt { get; set; } = string.Empty;

        [JsonPropertyName("track")]
        public SpotifyTrackObject Track { get; set; } = new();
    }
}
