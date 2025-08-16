using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifySavedAlbumItem
    {
        [JsonPropertyName("added_at")]
        public string AddedAt { get; set; } = string.Empty;

        [JsonPropertyName("album")]
        public SpotifyAlbumObject Album { get; set; } = new();
    }
}
