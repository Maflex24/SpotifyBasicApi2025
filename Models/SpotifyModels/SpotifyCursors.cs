using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyCursors
    {
        [JsonPropertyName("after")]
        public string? After { get; set; }

        [JsonPropertyName("before")]
        public string? Before { get; set; }
    }
}
