namespace SpotifyBasicApi2025.Models
{
    public class PlaylistModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int TotalTracks { get; set; }

        public List<SpotifyImage> Images { get; set; } = new List<SpotifyImage>();

        public long? FolderId { get; set; }
    }
}


