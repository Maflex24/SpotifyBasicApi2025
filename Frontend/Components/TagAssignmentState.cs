namespace Frontend.Components
{
    public sealed class TagAssignmentState
    {
        public string ItemType { get; set; } = string.Empty;
        public string SpotifyItemId { get; set; } = string.Empty;
        public List<TagBadge> Tags { get; set; } = new();
    }
}
