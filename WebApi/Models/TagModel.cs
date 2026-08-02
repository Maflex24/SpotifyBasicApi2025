namespace SpotifyBasicApi2025.Models
{
    public class TagModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ForAlbum { get; set; }
        public bool ForArtist { get; set; }
        public bool ForTrack { get; set; }
        public bool ForPlaylist { get; set; }
        public long TagGroupId { get; set; }
        public int AssignmentCount { get; set; }
    }
}
