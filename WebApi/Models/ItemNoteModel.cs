namespace SpotifyBasicApi2025.Models
{
    public class ItemNoteModel
    {
        public string ItemType { get; set; } = string.Empty;
        public string SpotifyItemId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
