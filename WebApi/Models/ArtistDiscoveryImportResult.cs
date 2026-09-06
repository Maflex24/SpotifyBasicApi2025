namespace SpotifyBasicApi2025.Models
{
    public class ArtistDiscoveryImportResult
    {
        public string ArtistId { get; set; } = string.Empty;
        public bool AddedToLibrary { get; set; }
        public int AlbumsFound { get; set; }
        public int AlbumsAdded { get; set; }
        public int AlbumsAlreadyInLibrary { get; set; }
        public int AlbumsInLibrary { get; set; }
        public int AlbumsFailed { get; set; }
        public string? Error { get; set; }
    }
}
