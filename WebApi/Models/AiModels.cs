namespace SpotifyBasicApi2025.Models;

public sealed class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ItemType { get; set; }
    public string? SpotifyItemId { get; set; }
    public List<AiConversationMessage> Conversation { get; set; } = new();
}

public sealed class AiConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class AiChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string? Notice { get; set; }
    public List<AiResultItem> Items { get; set; } = new();
    public List<AiResearchFact> Facts { get; set; } = new();
    public List<AiSourceLink> Sources { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<AiTagProposal> TagProposals { get; set; } = new();
}

public sealed class AiResearchFact
{
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public sealed class AiResultItem
{
    public string Name { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public decimal? Score { get; set; }
    public string? Metric { get; set; }
    public string? Url { get; set; }
}

public sealed class AiSourceLink
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

public sealed class AiTagProposal
{
    public string Name { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public long? ExistingTagId { get; set; }
}

public sealed class AiSettingsModel
{
    public bool Enabled { get; set; }
    public string AccessMode { get; set; } = "allowlist";
    public List<string> AllowedUserIds { get; set; } = new();
    public int DailyRequestLimit { get; set; }
    public string DefaultModel { get; set; } = string.Empty;
    public bool HasOpenAiApiKey { get; set; }
    public bool HasLastFmApiKey { get; set; }
    public string? OpenAiApiKey { get; set; }
    public string? LastFmApiKey { get; set; }
    public bool ClearOpenAiApiKey { get; set; }
    public bool ClearLastFmApiKey { get; set; }
}

public sealed class AiAccessModel
{
    public bool Allowed { get; set; }
    public string? Reason { get; set; }
    public int RemainingRequestsToday { get; set; }
}

public sealed class AiModelOption
{
    public string Id { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class AiUsageSummary
{
    public int RequestsToday { get; set; }
    public int InputTokensToday { get; set; }
    public int OutputTokensToday { get; set; }
}
