using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibTrackUserAssign
    {
        [Key]
        public long Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string TrackId { get; set; } = string.Empty;

        public string TrackName { get; set; } = string.Empty;

        public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;

        // Właściwości nawigacyjne
        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;

        [ForeignKey("TrackId")]
        public virtual SpTrack Track { get; set; } = null!;
    }
}
