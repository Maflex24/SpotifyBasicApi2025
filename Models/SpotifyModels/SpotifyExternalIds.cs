using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyExternalIds
    {
        [JsonPropertyName("isrc")]
        public string? Isrc { get; set; }

        [JsonPropertyName("ean")]
        public string? Ean { get; set; }

        [JsonPropertyName("upc")]
        public string? Upc { get; set; }
    }
}
