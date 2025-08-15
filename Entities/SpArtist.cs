using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities
{
    public class SpArtist
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public string? Uri { get; set; }
        
        public string? Href { get; set; }
        
        public string? Genres { get; set; } // Przechowywane jako string z separatorem ","
        
        public int Popularity { get; set; }
        
        public string? Images { get; set; } // Przechowywane jako string z separatorem ","
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Relacje
        public virtual ICollection<SpAlbum> Albums { get; set; } = new List<SpAlbum>();
        
        public virtual ICollection<SpTrackArtist> TrackArtists { get; set; } = new List<SpTrackArtist>();
    }
}
