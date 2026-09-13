namespace SpotifyBasicApi2025.Models
{
    public class TagAssignmentHistoryModel
    {
        public long Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public long TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string TagGroupName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string SpotifyItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public float? PreviousStrength { get; set; }
        public float? NewStrength { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
