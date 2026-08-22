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
    public static string ToQuery(IEnumerable<TagStrengthFilterRule> rules) => string.Join(',', rules
        .Where(rule => !string.Equals(rule.Operator, "all", StringComparison.OrdinalIgnoreCase))
        .OrderBy(rule => rule.TagId)
        .Select(rule => $"{rule.TagId}:{rule.Operator}:{rule.Value.ToString(CultureInfo.InvariantCulture)}"));

    public static bool Equal(IReadOnlyList<TagStrengthFilterRule> left, IReadOnlyList<TagStrengthFilterRule> right) => left.Count == right.Count
        && left.OrderBy(rule => rule.TagId).Zip(right.OrderBy(rule => rule.TagId)).All(pair => pair.First.TagId == pair.Second.TagId
            && string.Equals(pair.First.Operator, pair.Second.Operator, StringComparison.OrdinalIgnoreCase)
            && pair.First.Value.Equals(pair.Second.Value));
}
