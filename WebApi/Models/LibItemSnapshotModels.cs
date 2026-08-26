namespace SpotifyBasicApi2025.Models;

public class LibItemSnapshotInput
{
    public string Title { get; set; } = string.Empty;
    public List<string> Artists { get; set; } = new();
    public string? AlbumTitle { get; set; }
    public string? OwnerName { get; set; }
    public string? SpotifyUri { get; set; }
}

public sealed class LibItemSnapshotModel : LibItemSnapshotInput
{
    public string ItemType { get; set; } = string.Empty;
    public string SpotifyItemId { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; }
    public DateTime LastConfirmedAtUtc { get; set; }
    public List<TagAssignmentStateModel> Tags { get; set; } = new();
    public string? Note { get; set; }
}
