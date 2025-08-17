namespace SpotifyBasicApi2025.Entities
{
    public class LibTagGroup
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ForAlbum { get; set; }
        public bool ForArtist { get; set; }
        public bool ForTrack { get; set; }
        public bool ForPlaylist { get; set; }
        public string UserId { get; set; }

        public virtual ICollection<LibTag> Tags { get; set; }
    }
}