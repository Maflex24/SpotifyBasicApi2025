using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

public sealed class AiSettingsService(
    DatabaseContext context,
    UserService userService,
    AppUserAuthService appUserAuthService,
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector _secretProtector = dataProtectionProvider.CreateProtector("SpotifyBasicApi2025.AiSettingsSecrets.v1");

    public async Task<AiSettings> GetSettingsAsync()
    {
        var settings = await context.AiSettings.SingleOrDefaultAsync(x => x.Id == 1);
        if (settings != null) return settings;
        settings = new AiSettings { AllowedUserIds = "[]" };
        context.AiSettings.Add(settings);
        await context.SaveChangesAsync();
        return settings;
    }

    public async Task<AiSettingsModel> GetModelAsync()
    {
        var settings = await GetSettingsAsync();
        return ToModel(settings);
    }

    public async Task<AiAccessModel> GetCurrentAccessAsync()
    {
        var spotifyUserId = await userService.GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(spotifyUserId)) return new AiAccessModel { Reason = "Zaloguj się do Spotify, aby użyć asystenta." };
        var settings = await GetSettingsAsync();
        var allowedUsers = DeserializeUserIds(settings.AllowedUserIds);
        var allowed = settings.Enabled && (settings.AccessMode == "all" || (settings.AccessMode == "allowlist" && allowedUsers.Contains(spotifyUserId, StringComparer.Ordinal)));
        var from = DateTime.UtcNow.Date;
        var used = await context.AiUsageMetrics.CountAsync(x => x.UserId == spotifyUserId && x.CreatedAtUtc >= from);
        var remaining = Math.Max(0, settings.DailyRequestLimit - used);
        if (allowed && remaining == 0) allowed = false;
        return new AiAccessModel
        {
            Allowed = allowed,
            RemainingRequestsToday = remaining,
            Reason = allowed ? null : !settings.Enabled ? "Asystent AI jest obecnie wyłączony." : remaining == 0 ? "Wykorzystano dzienny limit zapytań." : "Twój użytkownik Spotify nie ma dostępu do asystenta AI."
        };
    }

    public async Task RequireAdministratorAsync() => await appUserAuthService.RequireAdministratorAsync();

    public async Task UpdateAsync(AiSettingsModel model)
    {
        await RequireAdministratorAsync();
        if (model.AccessMode is not ("allowlist" or "all")) throw new ArgumentException("Nieprawidłowy tryb dostępu.");
        if (model.DailyRequestLimit is < 1 or > 1000) throw new ArgumentException("Limit dzienny musi mieścić się w zakresie 1–1000.");
        if (string.IsNullOrWhiteSpace(model.DefaultModel)) throw new ArgumentException("Model domyślny jest wymagany.");

        var settings = await GetSettingsAsync();
        settings.Enabled = model.Enabled;
        settings.AccessMode = model.AccessMode;
        settings.AllowedUserIds = JsonSerializer.Serialize(model.AllowedUserIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal));
        settings.DailyRequestLimit = model.DailyRequestLimit;
        settings.DefaultModel = model.DefaultModel.Trim();
        if (model.ClearOpenAiApiKey) settings.OpenAiApiKeyProtected = null;
        else if (!string.IsNullOrWhiteSpace(model.OpenAiApiKey)) settings.OpenAiApiKeyProtected = _secretProtector.Protect(model.OpenAiApiKey.Trim());
        if (model.ClearLastFmApiKey) settings.LastFmApiKeyProtected = null;
        else if (!string.IsNullOrWhiteSpace(model.LastFmApiKey)) settings.LastFmApiKeyProtected = _secretProtector.Protect(model.LastFmApiKey.Trim());
        settings.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<string?> GetOpenAiApiKeyAsync() => Unprotect((await GetSettingsAsync()).OpenAiApiKeyProtected);
    public async Task<string?> GetLastFmApiKeyAsync() => Unprotect((await GetSettingsAsync()).LastFmApiKeyProtected);

    public async Task<List<AiModelOption>> GetAvailableModelsAsync()
    {
        await RequireAdministratorAsync();
        var apiKey = await GetOpenAiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey)) return new();
        try
        {
            var client = httpClientFactory.CreateClient("OpenAi");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await client.GetAsync("models");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var settings = await GetSettingsAsync();
            return json.RootElement.GetProperty("data").EnumerateArray()
                .Select(x => x.TryGetProperty("id", out var id) ? id.GetString() : null)
                .Where(id => !string.IsNullOrWhiteSpace(id) && id!.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => new AiModelOption { Id = id!, IsDefault = id == settings.DefaultModel }).ToList();
        }
        catch { return new(); }
    }

    public async Task<bool> RegisterUsageAsync(string model, int inputTokens, int outputTokens, string outcome)
    {
        var spotifyUserId = await userService.GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(spotifyUserId)) return false;
        var settings = await GetSettingsAsync();
        var used = await context.AiUsageMetrics.CountAsync(x => x.UserId == spotifyUserId && x.CreatedAtUtc >= DateTime.UtcNow.Date);
        if (used >= settings.DailyRequestLimit) return false;
        context.AiUsageMetrics.Add(new AiUsageMetric { UserId = spotifyUserId, Model = model, InputTokens = Math.Max(0, inputTokens), OutputTokens = Math.Max(0, outputTokens), Outcome = outcome, CreatedAtUtc = DateTime.UtcNow });
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AiUsageSummary> GetUsageSummaryAsync()
    {
        await RequireAdministratorAsync();
        var metrics = context.AiUsageMetrics.Where(x => x.CreatedAtUtc >= DateTime.UtcNow.Date);
        return new AiUsageSummary { RequestsToday = await metrics.CountAsync(), InputTokensToday = await metrics.SumAsync(x => (int?)x.InputTokens) ?? 0, OutputTokensToday = await metrics.SumAsync(x => (int?)x.OutputTokens) ?? 0 };
    }

    private string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return _secretProtector.Unprotect(value); }
        catch { return null; }
    }
    private static List<string> DeserializeUserIds(string value) { try { return JsonSerializer.Deserialize<List<string>>(value) ?? new(); } catch { return new(); } }
    private AiSettingsModel ToModel(AiSettings settings) => new()
    {
        Enabled = settings.Enabled,
        AccessMode = settings.AccessMode is "allowlist" or "all" ? settings.AccessMode : "allowlist",
        AllowedUserIds = DeserializeUserIds(settings.AllowedUserIds),
        DailyRequestLimit = settings.DailyRequestLimit,
        DefaultModel = settings.DefaultModel,
        HasOpenAiApiKey = !string.IsNullOrWhiteSpace(Unprotect(settings.OpenAiApiKeyProtected)),
        HasLastFmApiKey = !string.IsNullOrWhiteSpace(Unprotect(settings.LastFmApiKeyProtected))
    };
}
