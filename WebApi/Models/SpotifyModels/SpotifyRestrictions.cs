using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyRestrictions
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
