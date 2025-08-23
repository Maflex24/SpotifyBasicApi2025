using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibPlaylistFolderAssign
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string PlaylistId { get; set; } = string.Empty;

        public long? FolderId { get; set; }

        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")] public virtual SpUser User { get; set; } = null!;
        [ForeignKey("PlaylistId")] public virtual SpPlaylist Playlist { get; set; } = null!;
        [ForeignKey("FolderId")] public virtual LibPlaylistFolder? Folder { get; set; }
    }
}


