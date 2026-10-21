using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;

namespace SpotifyBasicApi2025.Services;

public static class TagAssignmentDateSorting
{
    public static long[] ResolveTagIds(string? tags, string? includeTags, long? sortTagId, string? mode)
    {
        var ids = new HashSet<long>();
        foreach (var value in new[] { tags, includeTags })
        {
            foreach (var part in (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!long.TryParse(part, out var id) || id <= 0)
                    throw new ArgumentException("Nieprawidłowy tag do sortowania po dacie przypisania.");
                ids.Add(id);
            }
        }

        if (ids.Count == 0)
            throw new ArgumentException("Sortowanie po dacie przypisania wymaga włączonego filtra tagów.");

        if (mode == "latest")
            return ids.OrderBy(id => id).ToArray();
        if (mode is not null && mode != "single")
            throw new ArgumentException("Nieprawidłowy tryb sortowania po dacie przypisania.");
        if (!sortTagId.HasValue || !ids.Contains(sortTagId.Value))
            throw new ArgumentException("Wybierz tag do sortowania spośród włączonych filtrów.");

        return new[] { sortTagId.Value };
    }

    // All three library entities have mapped string Id and Name properties.
    public static IQueryable<T> Apply<T>(IQueryable<T> items, IQueryable<LibTagAssign> assignments,
        string userId, string itemType, long[] tagIds, bool descending) where T : class
    {
        var dated = items.Select(item => new
        {
            Item = item,
            AssignedAt = assignments
                .Where(assignment => assignment.UserId == userId && assignment.ItemType == itemType
                    && assignment.SpotifyItemId == EF.Property<string>(item, "Id") && tagIds.Contains(assignment.TagId))
                .Select(assignment => (DateTime?)assignment.CreatedAt)
                .Max()
        });

        var ordered = dated.OrderBy(row => row.AssignedAt == null);
        return (descending ? ordered.ThenByDescending(row => row.AssignedAt) : ordered.ThenBy(row => row.AssignedAt))
            .ThenBy(row => EF.Property<string>(row.Item, "Name"))
            .ThenBy(row => EF.Property<string>(row.Item, "Id"))
            .Select(row => row.Item);
    }
}
