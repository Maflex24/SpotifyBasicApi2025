using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibArtistUserAssign
    {
        [Key]
        public long Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string ArtistId { get; set; } = string.Empty;
        
        public string ArtistName { get; set; } = string.Empty;
        
        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
        
        // Właściwości nawigacyjne
        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;
        
        [ForeignKey("ArtistId")]
        public virtual SpArtist Artist { get; set; } = null!;
    }
}