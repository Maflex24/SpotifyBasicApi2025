using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities
{
    public class SpUser
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? Type { get; set; }

        public string? Uri { get; set; }

        public string? Href { get; set; }

        public string? Country { get; set; }

        public string? Email { get; set; }

        public string? Product { get; set; }

        public string? Images { get; set; } // Przechowywane jako string z separatorem ","

        public int Followers { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relacje
        public virtual ICollection<SpPlaylist> Playlists { get; set; } = new List<SpPlaylist>();

        public virtual ICollection<SpSavedTrack> SavedTracks { get; set; } = new List<SpSavedTrack>();

        public virtual ICollection<SpSavedAlbum> SavedAlbums { get; set; } = new List<SpSavedAlbum>();

        public virtual ICollection<LibArtistUserAssign> ArtistAssignments { get; set; } = new List<LibArtistUserAssign>();

        public virtual ICollection<LibAlbumUserAssign> AlbumAssignments { get; set; } = new List<LibAlbumUserAssign>();

        public virtual ICollection<LibTrackUserAssign> TrackAssignments { get; set; } = new List<LibTrackUserAssign>();
    }
}