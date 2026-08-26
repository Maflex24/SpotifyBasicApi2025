using Frontend.Components;

namespace Frontend;

public sealed class TagClipboardService
{
    public TagClipboardContent? Content { get; private set; }

    public event Action? Changed;

    public void Copy(string itemType, string spotifyItemId, string displayName, IEnumerable<TagBadge> tags)
    {
        var copiedTags = tags
            .Where(tag => tag.TagId > 0)
            .GroupBy(tag => tag.TagId)
            .Select(group => group.First())
            .OrderBy(tag => tag.TagName, StringComparer.CurrentCultureIgnoreCase)
            .Select(tag => new TagBadge
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                TagGroupName = tag.TagGroupName,
                Strength = tag.Strength
            })
            .ToList();

        if (copiedTags.Count == 0)
            return;

        Content = new TagClipboardContent(itemType, spotifyItemId, displayName, copiedTags);
        Changed?.Invoke();
    }

    public bool CanPasteTo(string itemType, string spotifyItemId, bool isInLibrary) =>
        isInLibrary
        && Content is { Tags.Count: > 0 }
        && !(string.Equals(Content.ItemType, itemType, StringComparison.OrdinalIgnoreCase)
             && string.Equals(Content.SpotifyItemId, spotifyItemId, StringComparison.Ordinal));
}

public sealed record TagClipboardContent(string ItemType, string SpotifyItemId, string DisplayName, IReadOnlyList<TagBadge> Tags);
