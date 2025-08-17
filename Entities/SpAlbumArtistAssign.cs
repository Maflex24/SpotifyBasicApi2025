using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class SpAlbumArtistAssign
    {
        [Key]
        public long Id { get; set; }

        public string AlbumId { get; set; } = string.Empty;

        public string ArtistId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }

        // Właściwości nawigacyjne
        [ForeignKey("AlbumId")]
        public virtual SpAlbum Album { get; set; } = null!;

        [ForeignKey("ArtistId")]
        public virtual SpArtist Artist { get; set; } = null!;
    }
}
