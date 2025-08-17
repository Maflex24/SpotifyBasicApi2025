using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpPlaylist
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public string? Uri { get; set; }
        
        public string? Href { get; set; }
        
        public string? Description { get; set; }
        
        public bool Collaborative { get; set; }
        
        public bool Public { get; set; }
        
        public int TotalTracks { get; set; }
        
        public string? Images { get; set; } // Przechowywane jako string z separatorem ","
        
        public string? SnapshotId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string? OwnerId { get; set; }
        
        // Relacje
        [ForeignKey("OwnerId")]
        public virtual SpUser? Owner { get; set; }
        
        public virtual ICollection<SpPlaylistTrack> PlaylistTracks { get; set; } = new List<SpPlaylistTrack>();
    }
}
