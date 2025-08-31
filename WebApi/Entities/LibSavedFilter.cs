using System;
using System.ComponentModel.DataAnnotations;

namespace SpotifyBasicApi2025.Entities
{
    public class LibSavedFilter
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string Scope { get; set; } = string.Empty; // tracks | albums | artists | playlist

        [MaxLength(128)]
        public string? ContextId { get; set; } // np. playlistId dla scope=playlist

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PayloadJson { get; set; } = string.Empty; // pełny stan filtrów jako JSON

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}

