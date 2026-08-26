namespace SpotifyBasicApi2025.Models;

public sealed class BulkTagAssignModel
{
    public string SpotifyItemId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public List<BulkTagAssignItemModel> Tags { get; set; } = new();
}

public sealed class BulkTagAssignItemModel
{
    public long TagId { get; set; }
    public float Strength { get; set; }
}
