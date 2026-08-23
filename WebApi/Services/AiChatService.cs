using System.Text;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

public sealed class AiChatService(
    AiSettingsService settingsService,
    MusicResearchService researchService,
    IAiChatCompletionProvider completionProvider,
    TagsService tagsService,
    UserService userService)
{
    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var access = await settingsService.GetCurrentAccessAsync();
        if (!access.Allowed) return new AiChatResponse { Notice = access.Reason ?? "Brak dostępu do asystenta." };
        if (string.IsNullOrWhiteSpace(request.ItemType) || string.IsNullOrWhiteSpace(request.SpotifyItemId))
            return new AiChatResponse { Notice = "Wybierz kontekst artysty, albumu lub utworu, aby asystent mógł sprawdzić źródła." };

        var intent = DetectIntent(request.Message);
        var packet = await researchService.ResearchAsync(intent, request.ItemType, request.SpotifyItemId, cancellationToken);
        if (packet == null) return new AiChatResponse { Intent = intent, Notice = "Nie znaleziono wybranego elementu w lokalnej bibliotece." };

        var response = new AiChatResponse { Intent = intent, Items = packet.Items, Sources = packet.Sources };
        if (intent is "genres" or "tag-suggestions")
        {
            response.Items = packet.Genres.Select(genre => new AiResultItem { Name = genre, Metric = "gatunek/tag MusicBrainz" }).ToList();
            if (intent == "tag-suggestions")
                response.TagProposals = await GetTagProposalsAsync(packet.Genres, request.ItemType);
        }
        response.Answer = BuildFallbackAnswer(intent, packet, response.Items);

        var settings = await settingsService.GetSettingsAsync();
        var research = BuildResearchText(intent, packet, response.Items);
        try
        {
            var completion = await completionProvider.CreateShortAnswerAsync(settings.DefaultModel, research, cancellationToken);
            if (completion != null)
            {
                response.Answer = completion.Text;
                await settingsService.RegisterUsageAsync(settings.DefaultModel, completion.InputTokens, completion.OutputTokens, "success");
            }
            else
            {
                response.Notice = "Pokazujemy odpowiedź opartą o źródła; streszczenie AI jest obecnie niedostępne.";
                await settingsService.RegisterUsageAsync(settings.DefaultModel, 0, 0, "fallback");
            }
        }
        catch
        {
            response.Notice = "Nie udało się pobrać streszczenia AI. Dane źródłowe pozostają dostępne.";
            await settingsService.RegisterUsageAsync(settings.DefaultModel, 0, 0, "error");
        }
        return response;
    }

    private async Task<List<AiTagProposal>> GetTagProposalsAsync(List<string> genres, string? itemType)
    {
        var userId = await userService.GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId)) return new();
        var tags = await tagsService.GetAllTagsAsync(userId);
        return genres.Take(8).Select(genre =>
        {
            var matching = tags.FirstOrDefault(tag => string.Equals(tag.Name, genre, StringComparison.OrdinalIgnoreCase) || tag.Aliases.Any(alias => string.Equals(alias, genre, StringComparison.OrdinalIgnoreCase)));
            return new AiTagProposal
            {
                Name = matching?.Name ?? genre,
                ExistingTagId = matching?.Id,
                Reason = matching == null ? "Gatunek/tag wskazany przez MusicBrainz; utwórz lub przypisz ręcznie." : "Pasuje do istniejącego tagu na podstawie MusicBrainz."
            };
        }).DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string DetectIntent(string message)
    {
        var value = (message ?? string.Empty).ToLowerInvariant();
        if (value.Contains("tag")) return "tag-suggestions";
        if (value.Contains("gatun") || value.Contains("genre")) return "genres";
        if (value.Contains("podobn") && (value.Contains("album") || value.Contains("pły"))) return "similar-albums";
        if (value.Contains("podobn") || value.Contains("poleć") || value.Contains("rekomend")) return "similar-artists";
        if (value.Contains("najlep") || value.Contains("ocen") || value.Contains("popular") || value.Contains("album")) return "artist-top-albums";
        return "genres";
    }

    private static string BuildFallbackAnswer(string intent, AiResearchPacket packet, List<AiResultItem> items) => intent switch
    {
        "genres" => items.Count == 0 ? $"Nie znaleziono gatunków dla „{packet.SubjectName}” w MusicBrainz." : $"MusicBrainz wskazuje dla „{packet.SubjectName}” następujące gatunki lub tagi: {string.Join(", ", items.Select(x => x.Name))}.",
        "tag-suggestions" => items.Count == 0 ? "Brakuje danych do zaproponowania tagów." : $"Poniższe propozycje wynikają z tagów i gatunków MusicBrainz; zatwierdź je ręcznie.",
        "artist-top-albums" => items.Count == 0 ? "Nie znaleziono rankingu Last.fm." : $"To najpopularniejsze albumy w Last.fm — ranking popularności, nie ocena: {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        "similar-artists" => items.Count == 0 ? "Nie znaleziono podobnych wykonawców w Last.fm." : $"Last.fm wskazuje podobnych wykonawców, m.in. {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        _ => items.Count == 0 ? "Nie znaleziono podobnych albumów." : $"Propozycje pochodzą z podobieństwa wykonawców Last.fm: {string.Join(", ", items.Take(3).Select(x => x.Name))}."
    };

    private static string BuildResearchText(string intent, AiResearchPacket packet, List<AiResultItem> items)
    {
        // Do modelu przekazujemy wyłącznie intencję oraz materiał pobrany z zewnętrznych źródeł.
        var text = new StringBuilder($"Intencja: {intent}. ");
        if (packet.Genres.Count > 0) text.Append("Gatunki/tagi MusicBrainz: ").Append(string.Join(", ", packet.Genres)).Append(". ");
        if (items.Count > 0) text.Append("Wyniki: ").Append(string.Join("; ", items.Take(8).Select(x => $"{x.Name} ({x.Metric}, {x.Score})"))).Append(".");
        return text.ToString();
    }
}
