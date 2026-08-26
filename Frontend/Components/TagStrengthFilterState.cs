namespace Frontend.Components;

using System.Globalization;

public sealed class TagStrengthFilterState
{
    public string Operator { get; set; } = "all";
    public float Value { get; set; } = 3f;

    public TagStrengthFilterState Clone() => new() { Operator = Operator, Value = Value };
}

public sealed class TagStrengthFilterRule
{
    public long TagId { get; set; }
    public string Operator { get; set; } = "all";
    public float Value { get; set; } = 3f;

    public TagStrengthFilterState ToState() => new() { Operator = Operator, Value = Value };
}

public readonly record struct TagStrengthValueChanged(long TagId, float Value);

public static class TagStrengthFilters
{
    public static TagStrengthFilterRule[] Capture(IEnumerable<long> tagIds, IReadOnlyDictionary<long, TagStrengthFilterState> states) => tagIds
        .Distinct()
        .OrderBy(id => id)
        .Select(id =>
        {
            var state = states.GetValueOrDefault(id) ?? new TagStrengthFilterState();
            return new TagStrengthFilterRule { TagId = id, Operator = NormalizeOperator(state.Operator), Value = NormalizeValue(state.Value) };
        })
        .ToArray();

    public static Dictionary<long, TagStrengthFilterState> Restore(
        IEnumerable<TagStrengthFilterRule>? rules,
        IEnumerable<long> includeTagIds,
        IEnumerable<long> excludeTagIds)
    {
        var activeIds = includeTagIds.Concat(excludeTagIds).ToHashSet();
        return (rules ?? Enumerable.Empty<TagStrengthFilterRule>())
            .Where(rule => activeIds.Contains(rule.TagId))
            .GroupBy(rule => rule.TagId)
            .ToDictionary(group => group.Key, group => new TagStrengthFilterState
            {
                Operator = NormalizeOperator(group.Last().Operator),
                Value = NormalizeValue(group.Last().Value)
            });
    }

    public static string ToQuery(IEnumerable<TagStrengthFilterRule> rules) => string.Join(',', rules
        .Where(rule => !string.Equals(rule.Operator, "all", StringComparison.OrdinalIgnoreCase))
        .OrderBy(rule => rule.TagId)
        .Select(rule => $"{rule.TagId}:{rule.Operator}:{rule.Value.ToString(CultureInfo.InvariantCulture)}"));

    public static bool Equal(IReadOnlyList<TagStrengthFilterRule> left, IReadOnlyList<TagStrengthFilterRule> right) => left.Count == right.Count
        && left.OrderBy(rule => rule.TagId).Zip(right.OrderBy(rule => rule.TagId)).All(pair => pair.First.TagId == pair.Second.TagId
            && string.Equals(pair.First.Operator, pair.Second.Operator, StringComparison.OrdinalIgnoreCase)
            && pair.First.Value.Equals(pair.Second.Value));

    private static string NormalizeOperator(string? value) => value?.ToLowerInvariant() is "gte" or "lte" ? value.ToLowerInvariant() : "all";

    private static float NormalizeValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 3f;

        value = Math.Clamp(value, 0.5f, 6f);
        return MathF.Round(value * 2f, MidpointRounding.AwayFromZero) / 2f;
    }
}
