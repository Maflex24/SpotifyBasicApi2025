using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibItemNote
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;

        public string SpotifyItemId { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;
    }
}
