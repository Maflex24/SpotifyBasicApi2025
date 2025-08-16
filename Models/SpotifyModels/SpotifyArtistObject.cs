using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyArtistObject
    {
        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("followers")]
        public SpotifyFollowers Followers { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public List<SpotifyImageObject> Images { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
    }
}
