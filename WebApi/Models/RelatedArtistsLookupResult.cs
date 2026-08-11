namespace SpotifyBasicApi2025.Models
{
    public sealed class RelatedArtistsLookupResult
    {
        public List<SpotifyArtistObject> Artists { get; set; } = new();

        public bool IsGenreFallback { get; set; }

        public string? Notice { get; set; }
    }
}
