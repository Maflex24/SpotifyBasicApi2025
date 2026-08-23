using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SpotifyBasicApi2025.Services;

public sealed record AiCompletion(string Text, int InputTokens, int OutputTokens);

public interface IAiChatCompletionProvider
{
    Task<AiCompletion?> CreateShortAnswerAsync(string model, string research, CancellationToken cancellationToken = default);
}

public sealed class OpenAiChatCompletionProvider(IHttpClientFactory httpClientFactory, AiSettingsService settingsService) : IAiChatCompletionProvider
{
    public async Task<AiCompletion?> CreateShortAnswerAsync(string model, string research, CancellationToken cancellationToken = default)
    {
        var key = await settingsService.GetOpenAiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key)) return null;

        var payload = new
        {
            model,
            input = "Jesteś asystentem biblioteki muzycznej. Odpowiedz po polsku w najwyżej 3 zdaniach. " +
                    "Używaj tylko faktów z pakietu badawczego. Nie dopowiadaj faktów i nie nazywaj popularności oceną.\n\n" + research,
            max_output_tokens = 240
        };
        var client = httpClientFactory.CreateClient("OpenAi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await client.PostAsync("responses", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var text = ReadOutputText(json.RootElement);
        if (string.IsNullOrWhiteSpace(text)) return null;
        var usage = json.RootElement.TryGetProperty("usage", out var usageNode) ? usageNode : default;
        var input = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("input_tokens", out var i) ? i.GetInt32() : 0;
        var output = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("output_tokens", out var o) ? o.GetInt32() : 0;
        return new AiCompletion(text, input, output);
    }

    private static string? ReadOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            return outputText.GetString();
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in content.EnumerateArray())
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    return text.GetString();
        }
        return null;
    }
}
