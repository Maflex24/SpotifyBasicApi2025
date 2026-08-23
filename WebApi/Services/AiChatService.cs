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

        var intent = DetectIntent(request.Message, request.ItemType);
        var packet = await researchService.ResearchAsync(intent, request.ItemType, request.SpotifyItemId, cancellationToken);
        if (packet == null) return new AiChatResponse { Intent = intent, Notice = "Nie znaleziono wybranego elementu w lokalnej bibliotece." };

        var response = new AiChatResponse
        {
            Intent = intent,
            Items = packet.Items,
            Facts = packet.Facts,
            Sources = packet.Sources,
            Warnings = packet.Warnings
        };
        if (intent is "genres" or "tag-suggestions")
        {
            response.Items = packet.Genres.Select(genre => new AiResultItem { Name = genre, Metric = "gatunek/tag MusicBrainz" }).ToList();
            if (intent == "tag-suggestions")
                response.TagProposals = await GetTagProposalsAsync(packet.Genres, request.ItemType);
        }
        response.Answer = BuildFallbackAnswer(intent, packet, response.Items);
        if (!packet.HasAnswerData)
        {
            response.Notice = packet.Warnings.FirstOrDefault() ?? "Nie znaleziono potwierdzonych danych źródłowych dla tego pytania.";
            return response;
        }

        var settings = await settingsService.GetSettingsAsync();
        var research = BuildResearchText(intent, request.ItemType, request.Message, packet, response.Items, SanitizeConversation(request.Conversation));
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

    private static string DetectIntent(string message, string? itemType)
    {
        var value = (message ?? string.Empty).ToLowerInvariant();
        var isAlbumContext = string.Equals(itemType?.Trim(), "album", StringComparison.OrdinalIgnoreCase);
        var isArtistContext = string.Equals(itemType?.Trim(), "artist", StringComparison.OrdinalIgnoreCase);
        if (value.Contains("tag")) return "tag-suggestions";
        if (value.Contains("gatun") || value.Contains("genre")) return "genres";
        if (value.Contains("podobn") && (value.Contains("album") || value.Contains("pły"))) return "similar-albums";
        if (value.Contains("podobn") || value.Contains("poleć") || value.Contains("rekomend")) return "similar-artists";
        if (value.Contains("najlep") || value.Contains("ocen") || value.Contains("popular") || value.Contains("najczęściej")) return "artist-top-albums";
        if (value.Contains("skład") || value.Contains("członk") || value.Contains("nadal gra") || value.Contains("gra w zespole") || (value.Contains("zesp") && value.Contains("gra")) || value.Contains("muzyk")) return "current-members";
        if (value.Contains("powsta") || value.Contains("założ") || value.Contains("utworz") || value.Contains("histori") || (isArtistContext && value.Contains("kiedy"))) return "artist-history";
        if (isAlbumContext || value.Contains("album") || value.Contains("płyta") || value.Contains("wydanie")) return "album-overview";
        return "overview";
    }

    private static string BuildFallbackAnswer(string intent, AiResearchPacket packet, List<AiResultItem> items) => intent switch
    {
        "genres" => items.Count == 0 ? $"Nie znaleziono gatunków dla „{packet.SubjectName}” w MusicBrainz." : $"MusicBrainz wskazuje dla „{packet.SubjectName}” następujące gatunki lub tagi: {string.Join(", ", items.Select(x => x.Name))}.",
        "tag-suggestions" => items.Count == 0 ? "Brakuje danych do zaproponowania tagów." : $"Poniższe propozycje wynikają z tagów i gatunków MusicBrainz; zatwierdź je ręcznie.",
        "artist-top-albums" => items.Count == 0 ? "Nie znaleziono rankingu Last.fm." : $"To najpopularniejsze albumy w Last.fm — ranking popularności, nie ocena: {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        "similar-artists" => items.Count == 0 ? "Nie znaleziono podobnych wykonawców w Last.fm." : $"Last.fm wskazuje podobnych wykonawców, m.in. {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        "current-members" => BuildFactsFallback(packet, new[] { "Obecny skład", "Dawny skład" }, "Nie znaleziono potwierdzonych danych o składzie w użytych źródłach."),
        "artist-history" => BuildFactsFallback(packet, new[] { "Historia", "Pochodzenie", "Profil" }, $"Nie znaleziono potwierdzonych danych historycznych o „{packet.SubjectName}”."),
        "overview" or "album-overview" => BuildFactsFallback(packet, new[] { "Opis", "Historia", "Pochodzenie", "Profil", "Wydanie" }, $"Nie znaleziono wystarczających danych źródłowych, aby opowiedzieć o „{packet.SubjectName}”."),
        _ => items.Count == 0 ? "Nie znaleziono podobnych albumów." : $"Propozycje pochodzą z podobieństwa wykonawców Last.fm: {string.Join(", ", items.Take(3).Select(x => x.Name))}."
    };

    private static string BuildFactsFallback(AiResearchPacket packet, IEnumerable<string> categories, string noData)
    {
        var facts = packet.Facts.Where(fact => categories.Contains(fact.Category)).Take(5).ToList();
        if (facts.Count == 0 && packet.Genres.Count > 0)
            return $"Dla „{packet.SubjectName}” MusicBrainz podaje gatunki lub tagi: {string.Join(", ", packet.Genres)}.";
        return facts.Count == 0 ? noData : string.Join(". ", facts.Select(fact => $"{fact.Label}: {fact.Value}")) + ".";
    }

    private static string BuildResearchText(string intent, string? itemType, string message, AiResearchPacket packet, List<AiResultItem> items, List<AiConversationMessage> conversation)
    {
        // Do modelu przekazujemy tylko aktualnie wybrany element, pytanie i krótki wątek oraz materiał zewnętrznych źródeł, nigdy całą prywatną bibliotekę.
        var normalizedType = itemType?.Trim().ToLowerInvariant() switch { "artist" => "artysta", "album" => "album", "track" => "utwór", _ => "element" };
        var artist = string.IsNullOrWhiteSpace(packet.ArtistName) ? string.Empty : $"; wykonawca: {packet.ArtistName}";
        var currentQuestion = message.Trim()[..Math.Min(message.Trim().Length, 1_200)];
        var text = new StringBuilder($"Kontekst: {normalizedType} „{packet.SubjectName}”{artist}. Intencja: {intent}. Bieżące pytanie użytkownika: {currentQuestion}. ");
        text.Append("Odpowiadaj po polsku, w najwyżej 4 krótkich zdaniach. Używaj wyłącznie faktów z pakietu. Nie zgaduj, nie uzupełniaj wiedzą własną i nie traktuj braku danych jako zaprzeczenia. Dla składu bez daty końcowej użyj sformułowania „według danych MusicBrainz”. ");
        if (packet.Genres.Count > 0) text.Append("Gatunki/tagi MusicBrainz: ").Append(string.Join(", ", packet.Genres)).Append(". ");
        if (packet.Facts.Count > 0)
        {
            text.Append("Potwierdzone fakty: ");
            text.Append(string.Join("; ", packet.Facts.Take(16).Select(fact => $"{fact.Label}: {fact.Value} [źródło: {fact.SourceName}, {fact.SourceUrl}]"))).Append(". ");
        }
        if (items.Count > 0) text.Append("Wyniki: ").Append(string.Join("; ", items.Take(8).Select(x => $"{x.Name} ({x.Metric}, {x.Score})"))).Append(".");
        if (conversation.Count > 0)
        {
            text.Append("\n\nPoprzednie wiadomości w tym samym wątku (służą wyłącznie do rozumienia odniesień; nie są dodatkowymi źródłami faktów):\n");
            foreach (var conversationMessage in conversation)
                text.Append(conversationMessage.Role == "assistant" ? "Asystent: " : "Użytkownik: ").Append(conversationMessage.Text).Append('\n');
        }
        return text.ToString();
    }

    private static List<AiConversationMessage> SanitizeConversation(IEnumerable<AiConversationMessage>? messages) =>
        (messages ?? Enumerable.Empty<AiConversationMessage>())
            .Where(conversationMessage => conversationMessage.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(conversationMessage.Text))
            .TakeLast(6)
            .Select(conversationMessage => new AiConversationMessage
            {
                Role = conversationMessage.Role,
                Text = conversationMessage.Text.Trim()[..Math.Min(conversationMessage.Text.Trim().Length, 1_200)]
            })
            .ToList();
}
