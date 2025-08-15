using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpAlbum
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public string? Uri { get; set; }
        
        public string? Href { get; set; }
        
        public string AlbumType { get; set; } = string.Empty;
        
        public int TotalTracks { get; set; }
        
        public string? AvailableMarkets { get; set; } // Przechowywane jako string z separatorem ","
        
        public string? ReleaseDate { get; set; }
        
        public string? ReleaseDatePrecision { get; set; }
        
        public string? Images { get; set; } // Przechowywane jako string z separatorem ","
        
        public int Popularity { get; set; }
        
        public string? Label { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string? ArtistId { get; set; }
        
        // Relacje
        [ForeignKey("ArtistId")]
        public virtual SpArtist? Artist { get; set; }
        
        public virtual ICollection<SpTrack> Tracks { get; set; } = new List<SpTrack>();
    }
}
