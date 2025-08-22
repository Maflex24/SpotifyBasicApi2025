using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibPlaylistFolder
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;

        public long? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual LibPlaylistFolder? ParentFolder { get; set; }

        public virtual ICollection<LibPlaylistFolder> Children { get; set; } = new List<LibPlaylistFolder>();

        public virtual ICollection<SpPlaylist> Playlists { get; set; } = new List<SpPlaylist>();
    }
}