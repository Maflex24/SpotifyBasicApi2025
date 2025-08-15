using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpTrack
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public string? Uri { get; set; }
        
        public string? Href { get; set; }
        
        public int DurationMs { get; set; }
        
        public bool Explicit { get; set; }
        
        public int DiscNumber { get; set; }
        
        public int TrackNumber { get; set; }
        
        public string? AvailableMarkets { get; set; } // Przechowywane jako string z separatorem ","
        
        public int Popularity { get; set; }
        
        public bool IsPlayable { get; set; }
        
        public bool IsLocal { get; set; }
        
        public string? PreviewUrl { get; set; }
        
        public string? Images { get; set; } // Przechowywane jako string z separatorem ","
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string? AlbumId { get; set; }
        
        // Relacje
        [ForeignKey("AlbumId")]
        public virtual SpAlbum? Album { get; set; }
        
        public virtual ICollection<SpTrackArtist> TrackArtists { get; set; } = new List<SpTrackArtist>();
        
        public virtual ICollection<SpPlaylistTrack> PlaylistTracks { get; set; } = new List<SpPlaylistTrack>();
    }
}
