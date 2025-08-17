namespace SpotifyBasicApi2025.Models
{
    public class TagGroupModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ForAlbum { get; set; }
        public bool ForArtist { get; set; }
        public bool ForTrack { get; set; }
        public bool ForPlaylist { get; set; }
    }
}