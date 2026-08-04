namespace SpotifyBasicApi2025.Entities
{
    public class LibTag
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ForAlbum { get; set; }
        public bool ForArtist { get; set; }
        public bool ForTrack { get; set; }
        public bool ForPlaylist { get; set; }
        public bool IsPinned { get; set; }
        public long TagGroupId { get; set; }

        public virtual LibTagGroup TagGroup { get; set; } = null!;
        public virtual ICollection<LibTagAlias> Aliases { get; set; } = new List<LibTagAlias>();
    }
}
