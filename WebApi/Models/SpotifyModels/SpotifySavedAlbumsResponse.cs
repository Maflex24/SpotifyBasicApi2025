using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifySavedAlbumsResponse
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<SpotifySavedAlbumItem> Items { get; set; } = new();

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
