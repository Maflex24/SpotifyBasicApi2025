using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Frontend;

public sealed class AiChatDrawerService(HttpClient http, IJSRuntime js)
{
    private const string SessionStorageKey = "spotify-library.ai-chat-drawer.v1";
    private Task? _initializationTask;

    public event Action? Changed;
    public bool AccessLoaded { get; private set; }
    public bool IsAllowed { get; private set; }
    public int RemainingRequestsToday { get; private set; }
    public bool IsOpen { get; private set; }
    public AiChatContext? Context { get; private set; }
    public List<AiDrawerMessage> Messages { get; private set; } = new();

    public Task InitializeAsync() => _initializationTask ??= InitializeCoreAsync();

    public async Task OpenAsync(AiChatContext context)
    {
        if (!AccessLoaded || !IsAllowed) return;

        if (Context?.Key != context.Key)
        {
            Context = context;
            Messages = new();
        }

        IsOpen = true;
        await PersistAsync();
        NotifyChanged();
    }

    public async Task CloseAsync()
    {
        IsOpen = false;
        await PersistAsync();
        NotifyChanged();
    }

    public async Task ClearAsync()
    {
        IsOpen = false;
        Context = null;
        Messages = new();
        await PersistAsync();
        NotifyChanged();
    }

    public async Task AddMessageAsync(AiDrawerMessage message)
    {
        Messages.Add(message);
        await PersistAsync();
        NotifyChanged();
    }

    public Task RefreshAccessAsync() => RefreshAccessAsync(notify: true);

    private async Task RefreshAccessAsync(bool notify)
    {
        try
        {
            var access = await http.GetFromJsonAsync<AiAccess>("api/ai/access");
            IsAllowed = access?.Allowed == true;
            RemainingRequestsToday = access?.RemainingRequestsToday ?? 0;
        }
        catch
        {
            IsAllowed = false;
            RemainingRequestsToday = 0;
        }

        AccessLoaded = true;
        if (!IsAllowed) IsOpen = false;
        if (notify) NotifyChanged();
    }

    public async Task PersistAsync()
    {
        try
        {
            // Wiadomości Blind mogą zawierać świadomie ujawnioną tożsamość elementu, więc nie utrwalamy ich między odświeżeniami.
            var storedMessages = Context?.IsBlind == true ? new List<AiDrawerMessage>() : Messages;
            var state = new AiDrawerStoredState { IsOpen = IsOpen, Context = Context, Messages = storedMessages };
            await js.InvokeVoidAsync("sessionStorage.setItem", SessionStorageKey, System.Text.Json.JsonSerializer.Serialize(state));
        }
        catch { }
    }

    private async Task InitializeCoreAsync()
    {
        await RefreshAccessAsync(notify: false);
        if (IsAllowed)
        {
            try
            {
                var json = await js.InvokeAsync<string?>("sessionStorage.getItem", SessionStorageKey);
                var stored = string.IsNullOrWhiteSpace(json) ? null : System.Text.Json.JsonSerializer.Deserialize<AiDrawerStoredState>(json);
                if (stored?.Context is { ItemType: "artist" or "album" or "track" } && !string.IsNullOrWhiteSpace(stored.Context.SpotifyItemId))
                {
                    Context = stored.Context;
                    Messages = stored.Messages.TakeLast(20).ToList();
                    IsOpen = stored.IsOpen;
                }
            }
            catch { }
        }

        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();

    private sealed class AiDrawerStoredState
    {
        public bool IsOpen { get; set; }
        public AiChatContext? Context { get; set; }
        public List<AiDrawerMessage> Messages { get; set; } = new();
    }
}

public sealed class AiChatContext
{
    public string ItemType { get; set; } = string.Empty;
    public string SpotifyItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsBlind { get; set; }
    public string Key => $"{(IsBlind ? "blind" : "normal")}:{ItemType}:{SpotifyItemId}";
    public string Label => IsBlind ? ItemType switch { "artist" => "Ukryty artysta", "album" => "Ukryty album", "track" => "Ukryty utwór", _ => "Ukryty element" } : ItemType switch { "artist" => "Artysta", "album" => "Album", "track" => "Utwór", _ => "Element" };
    public string HeaderText => IsBlind ? "Tryb Blind" : DisplayName;
}

public sealed class AiDrawerMessage
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public AiDrawerResponse? Response { get; set; }
}

public sealed class AiDrawerResponse
{
    public string? Answer { get; set; }
    public string? Notice { get; set; }
    public List<AiDrawerItem> Items { get; set; } = new();
    public List<AiDrawerFact> Facts { get; set; } = new();
    public List<AiDrawerSource> Sources { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<AiDrawerTagProposal> TagProposals { get; set; } = new();
}

public sealed class AiDrawerItem { public string Name { get; set; } = string.Empty; public string? Subtitle { get; set; } public decimal? Score { get; set; } public string? Metric { get; set; } public string? Url { get; set; } }
public sealed class AiDrawerFact { public string Category { get; set; } = string.Empty; public string Label { get; set; } = string.Empty; public string Value { get; set; } = string.Empty; public string SourceName { get; set; } = string.Empty; public string SourceUrl { get; set; } = string.Empty; }
public sealed class AiDrawerSource { public string Name { get; set; } = string.Empty; public string Url { get; set; } = string.Empty; public string? Detail { get; set; } }
public sealed class AiDrawerTagProposal { public string Name { get; set; } = string.Empty; public string Reason { get; set; } = string.Empty; public long? ExistingTagId { get; set; } public bool Assigned { get; set; } }
public sealed class AiAccess { public bool Allowed { get; set; } public int RemainingRequestsToday { get; set; } }
