using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpSavedTrack
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime AddedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string UserId { get; set; } = string.Empty;
        
        public string TrackId { get; set; } = string.Empty;
        
        // Relacje
        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;
        
        [ForeignKey("TrackId")]
        public virtual SpTrack Track { get; set; } = null!;
    }
}
