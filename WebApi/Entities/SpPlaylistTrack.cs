using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpPlaylistTrack
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime AddedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Klucze obce
        public string PlaylistId { get; set; } = string.Empty;
        
        public string TrackId { get; set; } = string.Empty;
        
        // Relacje
        [ForeignKey("PlaylistId")]
        public virtual SpPlaylist Playlist { get; set; } = null!;
        
        [ForeignKey("TrackId")]
        public virtual SpTrack Track { get; set; } = null!;
    }
}
