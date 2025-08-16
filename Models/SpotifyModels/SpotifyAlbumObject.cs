using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class SpotifyAlbumObject
    {
        [JsonPropertyName("album_type")]
        public string AlbumType { get; set; } = string.Empty;

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string> AvailableMarkets { get; set; } = new();

        [JsonPropertyName("external_urls")]
        public SpotifyExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public List<SpotifyImageObject> Images { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = string.Empty;

        [JsonPropertyName("release_date_precision")]
        public string ReleaseDatePrecision { get; set; } = string.Empty;

        [JsonPropertyName("restrictions")]
        public SpotifyRestrictions? Restrictions { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("artists")]
        public List<SpotifyArtistObject> Artists { get; set; } = new();

        [JsonPropertyName("tracks")]
        public SpotifyTracksPagingObject Tracks { get; set; } = new();

        [JsonPropertyName("copyrights")]
        public List<SpotifyCopyrightObject> Copyrights { get; set; } = new();

        [JsonPropertyName("external_ids")]
        public SpotifyExternalIds ExternalIds { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }
    }
}
