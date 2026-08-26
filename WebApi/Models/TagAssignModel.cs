namespace SpotifyBasicApi2025.Models
{
    public class TagAssignModel
    {
        public long TagId { get; set; }

        public string SpotifyItemId { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty; // artist | album | track | playlist

        public float Strength { get; set; } = 3.0f; // 0.0 - 6.0

        public LibItemSnapshotInput? Snapshot { get; set; }
    }
}


