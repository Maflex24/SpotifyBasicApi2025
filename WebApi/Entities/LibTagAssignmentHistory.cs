using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities
{
    /// <summary>
    /// Immutable audit trail for changes to tag assignments. Values are snapshots so the
    /// entry remains meaningful after a tag or a Spotify item is removed or renamed.
    /// </summary>
    public class LibTagAssignmentHistory
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(255)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(32)]
        public string Action { get; set; } = string.Empty;

        public long TagId { get; set; }

        [Required, MaxLength(255)]
        public string TagName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string TagGroupName { get; set; } = string.Empty;

        [Required, MaxLength(16)]
        public string ItemType { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string SpotifyItemId { get; set; } = string.Empty;

        [Required, MaxLength(512)]
        public string ItemName { get; set; } = string.Empty;

        public float? PreviousStrength { get; set; }

        public float? NewStrength { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
