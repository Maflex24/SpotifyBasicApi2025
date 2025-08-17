using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpTrackArtist
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string TrackId { get; set; } = string.Empty;
        
        public string ArtistId { get; set; } = string.Empty;
        
        // Relacje
        [ForeignKey("TrackId")]
        public virtual SpTrack Track { get; set; } = null!;
        
        [ForeignKey("ArtistId")]
        public virtual SpArtist Artist { get; set; } = null!;
    }
}
