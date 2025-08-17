using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyFollowedArtistsResponse
    {
        [JsonPropertyName("artists")]
        public SpotifyArtistsPagingObject Artists { get; set; } = new();
    }
}
