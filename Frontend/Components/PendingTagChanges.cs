namespace Frontend.Components;

internal sealed class PendingTagChanges
{
    public const string RefreshFailed = "Nie udało się odświeżyć wyników. Kliknij Zastosuj, aby ponowić.";

    private readonly Dictionary<(string ItemType, string Id), List<TagBadge>> changes = new();

    public bool Any => changes.Count > 0;
    public bool AffectsPagination { get; private set; }
    public long Revision { get; private set; }

    public bool Contains(string itemType, string id) => changes.ContainsKey((itemType, id));

    public void Record(string itemType, string id, List<TagBadge> tags, bool affectsPagination = false)
    {
        if (changes.TryGetValue((itemType, id), out var previous) && SameTags(previous, tags))
            return;

        changes[(itemType, id)] = tags;
        AffectsPagination |= affectsPagination;
        Revision++;
    }

    public List<TagBadge>? GetTags(string itemType, string id) => changes.GetValueOrDefault((itemType, id));

    public void Clear()
    {
        changes.Clear();
        AffectsPagination = false;
    }

    public static bool SameTags(IEnumerable<TagBadge>? first, IEnumerable<TagBadge> second) =>
        (first ?? Array.Empty<TagBadge>()).Select(Values).ToHashSet().SetEquals(second.Select(Values));

    public static bool TouchesTags(IEnumerable<TagBadge>? previous, IEnumerable<TagBadge> next, IEnumerable<long> relevantTagIds)
    {
        var ids = relevantTagIds.ToHashSet();
        return !SameTags(previous?.Where(tag => ids.Contains(tag.TagId)), next.Where(tag => ids.Contains(tag.TagId)));
    }

    private static (long, string, string, float) Values(TagBadge tag) =>
        (tag.TagId, tag.TagName, tag.TagGroupName, tag.Strength);
}
