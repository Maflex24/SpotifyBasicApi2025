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

        var intent = DetectIntent(request.Message, request.ItemType, request.IsBlind);
        if (request.IsBlind && intent == "identity-reveal")
        {
            var identity = await researchService.GetIdentityAsync(request.ItemType, request.SpotifyItemId);
            return identity == null
                ? new AiChatResponse { Intent = intent, Notice = "Nie znaleziono wybranego elementu w lokalnej bibliotece." }
                : new AiChatResponse { Intent = intent, Answer = BuildIdentityAnswer(identity) };
        }

        var packet = await researchService.ResearchAsync(intent, request.ItemType, request.SpotifyItemId, cancellationToken);
        if (packet == null) return new AiChatResponse { Intent = intent, Notice = "Nie znaleziono wybranego elementu w lokalnej bibliotece." };

        var facts = GetVisibleFacts(packet, request.IsBlind);
        var items = GetVisibleItems(packet.Items, packet, request.IsBlind);
        var warnings = GetVisibleWarnings(packet.Warnings, packet, request.IsBlind);

        var response = new AiChatResponse
        {
            Intent = intent,
            Items = items,
            Facts = facts,
            Sources = packet.Sources,
            Warnings = warnings
        };
        if (intent is "genres" or "tag-suggestions")
        {
            response.Items = packet.Genres.Select(genre => new AiResultItem { Name = genre, Metric = "gatunek/tag MusicBrainz" }).ToList();
            if (intent == "tag-suggestions")
                response.TagProposals = await GetTagProposalsAsync(packet.Genres, request.ItemType);
        }
        response.Answer = BuildFallbackAnswer(intent, packet, response.Items, facts, request.IsBlind);
        if (!HasVisibleAnswerData(intent, packet, facts, response.Items))
        {
            response.Notice = warnings.FirstOrDefault() ?? (request.IsBlind ? "W trybie Blind nie ma dostępnych anonimowych danych dla tego pytania." : "Nie znaleziono potwierdzonych danych źródłowych dla tego pytania.");
            return response;
        }

        var settings = await settingsService.GetSettingsAsync();
        var history = request.IsBlind ? new List<AiConversationMessage>() : SanitizeConversation(request.Conversation);
        var research = BuildResearchText(intent, request.ItemType, request.Message, packet, facts, response.Items, history, request.IsBlind);
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

    private static string DetectIntent(string message, string? itemType, bool isBlind)
    {
        var value = (message ?? string.Empty).ToLowerInvariant();
        var isAlbumContext = string.Equals(itemType?.Trim(), "album", StringComparison.OrdinalIgnoreCase);
        var isArtistContext = string.Equals(itemType?.Trim(), "artist", StringComparison.OrdinalIgnoreCase);
        if (isBlind && IsIdentityRevealRequest(value)) return "identity-reveal";
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

    private static string BuildFallbackAnswer(string intent, AiResearchPacket packet, List<AiResultItem> items, List<AiResearchFact> facts, bool isBlind) => intent switch
    {
        "genres" => items.Count == 0 ? isBlind ? "Nie znaleziono gatunków dla ukrytego elementu w MusicBrainz." : $"Nie znaleziono gatunków dla „{packet.SubjectName}” w MusicBrainz." : isBlind ? $"MusicBrainz wskazuje następujące gatunki lub tagi: {string.Join(", ", items.Select(x => x.Name))}." : $"MusicBrainz wskazuje dla „{packet.SubjectName}” następujące gatunki lub tagi: {string.Join(", ", items.Select(x => x.Name))}.",
        "tag-suggestions" => items.Count == 0 ? "Brakuje danych do zaproponowania tagów." : $"Poniższe propozycje wynikają z tagów i gatunków MusicBrainz; zatwierdź je ręcznie.",
        "artist-top-albums" => items.Count == 0 ? "Nie znaleziono rankingu Last.fm." : $"To najpopularniejsze albumy w Last.fm — ranking popularności, nie ocena: {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        "similar-artists" => items.Count == 0 ? "Nie znaleziono podobnych wykonawców w Last.fm." : $"Last.fm wskazuje podobnych wykonawców, m.in. {string.Join(", ", items.Take(3).Select(x => x.Name))}.",
        "current-members" => BuildFactsFallback(packet, facts, new[] { "Obecny skład", "Dawny skład" }, "Nie znaleziono potwierdzonych danych o składzie w użytych źródłach.", isBlind),
        "artist-history" => BuildFactsFallback(packet, facts, new[] { "Historia", "Pochodzenie", "Profil" }, isBlind ? "Nie znaleziono anonimowych danych historycznych o ukrytym elemencie." : $"Nie znaleziono potwierdzonych danych historycznych o „{packet.SubjectName}”.", isBlind),
        "overview" or "album-overview" => BuildFactsFallback(packet, facts, new[] { "Opis", "Historia", "Pochodzenie", "Profil", "Wydanie" }, isBlind ? "Nie znaleziono wystarczających anonimowych danych o ukrytym elemencie." : $"Nie znaleziono wystarczających danych źródłowych, aby opowiedzieć o „{packet.SubjectName}”.", isBlind),
        _ => items.Count == 0 ? "Nie znaleziono podobnych albumów." : $"Propozycje pochodzą z podobieństwa wykonawców Last.fm: {string.Join(", ", items.Take(3).Select(x => x.Name))}."
    };

    private static string BuildFactsFallback(AiResearchPacket packet, List<AiResearchFact> visibleFacts, IEnumerable<string> categories, string noData, bool isBlind)
    {
        var facts = visibleFacts.Where(fact => categories.Contains(fact.Category)).Take(5).ToList();
        if (facts.Count == 0 && packet.Genres.Count > 0)
            return isBlind ? $"MusicBrainz podaje gatunki lub tagi: {string.Join(", ", packet.Genres)}." : $"Dla „{packet.SubjectName}” MusicBrainz podaje gatunki lub tagi: {string.Join(", ", packet.Genres)}.";
        return facts.Count == 0 ? noData : string.Join(". ", facts.Select(fact => $"{fact.Label}: {fact.Value}")) + ".";
    }

    private static string BuildResearchText(string intent, string? itemType, string message, AiResearchPacket packet, List<AiResearchFact> facts, List<AiResultItem> items, List<AiConversationMessage> conversation, bool isBlind)
    {
        // Do modelu przekazujemy tylko aktualnie wybrany element, pytanie i krótki wątek oraz materiał zewnętrznych źródeł, nigdy całą prywatną bibliotekę.
        var normalizedType = itemType?.Trim().ToLowerInvariant() switch { "artist" => "artysta", "album" => "album", "track" => "utwór", _ => "element" };
        var currentQuestion = message.Trim()[..Math.Min(message.Trim().Length, 1_200)];
        var context = isBlind ? $"ukryty {normalizedType}" : $"{normalizedType} „{packet.SubjectName}”" + (string.IsNullOrWhiteSpace(packet.ArtistName) ? string.Empty : $"; wykonawca: {packet.ArtistName}");
        var text = new StringBuilder($"Kontekst: {context}. Intencja: {intent}. Bieżące pytanie użytkownika: {currentQuestion}. ");
        text.Append("Odpowiadaj po polsku, w najwyżej 4 krótkich zdaniach. Używaj wyłącznie faktów z pakietu. Nie zgaduj, nie uzupełniaj wiedzą własną i nie traktuj braku danych jako zaprzeczenia. Dla składu bez daty końcowej użyj sformułowania „według danych MusicBrainz”. ");
        if (isBlind) text.Append("Tryb Blind: pod żadnym pozorem nie podawaj nazwy albumu, artysty ani utworu; nie próbuj identyfikować ukrytego elementu. ");
        if (packet.Genres.Count > 0) text.Append("Gatunki/tagi MusicBrainz: ").Append(string.Join(", ", packet.Genres)).Append(". ");
        if (facts.Count > 0)
        {
            text.Append("Potwierdzone fakty: ");
            text.Append(string.Join("; ", facts.Take(16).Select(fact => $"{fact.Label}: {fact.Value} [źródło: {fact.SourceName}, {fact.SourceUrl}]"))).Append(". ");
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

    private static bool IsIdentityRevealRequest(string value) =>
        value.Contains("jaki to album") || value.Contains("jaka to płyta") || value.Contains("jaki to artysta") ||
        value.Contains("jaki to zesp") || value.Contains("co to za album") || value.Contains("co to za zesp") ||
        value.Contains("kto to jest") || value.Contains("podaj nazw") || value.Contains("ujawnij nazw");

    private static string BuildIdentityAnswer(AiIdentityContext identity) => identity.ItemType switch
    {
        "album" => string.IsNullOrWhiteSpace(identity.ArtistName)
            ? $"To album „{identity.SubjectName}”."
            : $"To album „{identity.SubjectName}” wykonawcy „{identity.ArtistName}”.",
        "artist" => $"To artysta lub zespół „{identity.SubjectName}”.",
        "track" => string.IsNullOrWhiteSpace(identity.ArtistName)
            ? $"To utwór „{identity.SubjectName}”."
            : $"To utwór „{identity.SubjectName}” wykonawcy „{identity.ArtistName}”.",
        _ => $"To element „{identity.SubjectName}”."
    };

    private static List<AiResearchFact> GetVisibleFacts(AiResearchPacket packet, bool isBlind)
    {
        if (!isBlind) return packet.Facts.ToList();
        return packet.Facts
            .Where(fact => fact.Category is not "Obecny skład" and not "Dawny skład" and not "Opis" and not "Link")
            .Where(fact => !ContainsIdentity(fact.Label, packet) && !ContainsIdentity(fact.Value, packet))
            .Select(fact => new AiResearchFact
            {
                Category = fact.Category,
                Label = fact.Label,
                Value = fact.Value,
                SourceName = fact.SourceName,
                SourceUrl = fact.SourceUrl
            })
            .ToList();
    }

    private static List<AiResultItem> GetVisibleItems(IEnumerable<AiResultItem> items, AiResearchPacket packet, bool isBlind) =>
        items.Where(item => !isBlind || (!ContainsIdentity(item.Name, packet) && !ContainsIdentity(item.Subtitle, packet)))
            .Select(item => new AiResultItem { Name = item.Name, Subtitle = item.Subtitle, Score = item.Score, Metric = item.Metric, Url = item.Url })
            .ToList();

    private static List<string> GetVisibleWarnings(IEnumerable<string> warnings, AiResearchPacket packet, bool isBlind) =>
        warnings.Select(warning => isBlind ? RedactIdentity(warning, packet) : warning).ToList();

    private static bool HasVisibleAnswerData(string intent, AiResearchPacket packet, List<AiResearchFact> facts, List<AiResultItem> items) => intent switch
    {
        "genres" or "tag-suggestions" => packet.Genres.Count > 0,
        "artist-top-albums" or "similar-artists" or "similar-albums" => items.Count > 0,
        "current-members" => facts.Any(fact => fact.Category is "Obecny skład" or "Dawny skład"),
        _ => facts.Count > 0 || packet.Genres.Count > 0
    };

    private static bool ContainsIdentity(string? text, AiResearchPacket packet)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return IdentityValues(packet).Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string RedactIdentity(string text, AiResearchPacket packet)
    {
        foreach (var value in IdentityValues(packet).OrderByDescending(value => value.Length))
            text = text.Replace(value, "ukryty element", StringComparison.OrdinalIgnoreCase);
        return text;
    }

    private static IEnumerable<string> IdentityValues(AiResearchPacket packet) => new[] { packet.SubjectName, packet.ArtistName }
        .Where(value => !string.IsNullOrWhiteSpace(value) && value.Length >= 3)
        .Select(value => value!);

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
