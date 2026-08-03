using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibTagAlias
    {
        public long Id { get; set; }

        public long TagId { get; set; }

        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string NormalizedName { get; set; } = string.Empty;

        [ForeignKey(nameof(TagId))]
        public virtual LibTag Tag { get; set; } = null!;
    }
}
