namespace Frontend.Components;

public static class TagDateSortSelection
{
    public static long[] Candidates(IEnumerable<long> includedTags, string? selectedTagFilter)
    {
        var ids = includedTags.ToHashSet();
        if (long.TryParse(selectedTagFilter, out var id))
            ids.Add(id);
        return ids.OrderBy(id => id).ToArray();
    }

    public static void Normalize(ref string sortKey, ref string sortOrder, ref string mode,
        ref string? selectedTagSort, IReadOnlyCollection<long> candidates)
    {
        if (sortKey != "tagAdded")
            return;
        if (candidates.Count == 0)
        {
            sortKey = "name";
            sortOrder = "asc";
            mode = "single";
            selectedTagSort = null;
            return;
        }
        if (mode == "latest")
            return;
        mode = "single";
        if (!long.TryParse(selectedTagSort, out var tagId) || !candidates.Contains(tagId))
            selectedTagSort = candidates.Count == 1 ? candidates.Single().ToString() : null;
    }

    public static void AppendQuery(List<string> query, string sortKey, string mode, string? selectedTagSort)
    {
        if (sortKey == "tagAdded")
            query.Add($"tagDateMode={mode}");
        if (!string.IsNullOrEmpty(selectedTagSort) && !(sortKey == "tagAdded" && mode == "latest"))
            query.Add($"sortTagId={selectedTagSort}");
    }

    public static string Label(string mode, string order, string? tagName) =>
        $"Data przypisania: {(mode == "latest" ? "najnowsze spośród wybranych tagów" : tagName ?? "wybierz tag")} · {(order == "desc" ? "od najnowszych" : "od najstarszych")}";
}
