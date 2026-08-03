using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibAlbumUserAssign
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string AlbumId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string AlbumName { get; set; } = string.Empty;

        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        // Właściwości nawigacyjne
        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;

        [ForeignKey("AlbumId")]
        public virtual SpAlbum Album { get; set; } = null!;
    }
}
