namespace Frontend.Components
{
    public sealed class ItemNoteState
    {
        public string ItemType { get; set; } = string.Empty;
        public string SpotifyItemId { get; set; } = string.Empty;
        public string? Content { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
