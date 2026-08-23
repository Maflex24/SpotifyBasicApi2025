using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

public sealed class MusicResearchService(DatabaseContext context, IHttpClientFactory httpClientFactory, AiSettingsService settingsService)
{
    private static readonly SemaphoreSlim MusicBrainzLock = new(1, 1);
    private static DateTime _lastMusicBrainzCallUtc = DateTime.MinValue;

    public async Task<AiResearchPacket?> ResearchAsync(string intent, string itemType, string spotifyItemId, CancellationToken cancellationToken = default)
    {
        var subject = await ResolveSubjectAsync(itemType, spotifyItemId);
        if (subject == null) return null;

        var packet = new AiResearchPacket { SubjectName = subject.Name, ArtistName = subject.ArtistName, Intent = intent };
        if (!string.IsNullOrWhiteSpace(subject.ArtistName))
        {
            var artist = await FindMusicBrainzEntityAsync("artist", subject.ArtistName, null, cancellationToken);
            if (artist == null) packet.Warnings.Add($"Nie udało się jednoznacznie dopasować artysty „{subject.ArtistName}” w MusicBrainz.");
            else await AddArtistResearchAsync(packet, artist, cancellationToken);
        }

        var relatedAlbumName = subject.IsAlbum ? subject.Name : subject.AlbumName;
        if (!string.IsNullOrWhiteSpace(relatedAlbumName) && !string.IsNullOrWhiteSpace(subject.ArtistName))
        {
            var album = await FindMusicBrainzEntityAsync("release-group", subject.ArtistName, relatedAlbumName, cancellationToken);
            if (album == null) packet.Warnings.Add($"Nie udało się jednoznacznie dopasować albumu „{relatedAlbumName}” w MusicBrainz.");
            else await AddAlbumResearchAsync(packet, album, cancellationToken);
        }

        // Nie wyszukujemy utworu tylko po tytule: bez stabilnego identyfikatora łatwo pomylić wersję lub remiks.
        if (intent is "artist-top-albums" or "similar-artists" or "similar-albums")
            await AddLastFmResearchAsync(packet, intent, subject.ArtistName ?? subject.Name, cancellationToken);

        packet.HasAnswerData = intent switch
        {
            "genres" or "tag-suggestions" => packet.Genres.Count > 0,
            "artist-top-albums" or "similar-artists" or "similar-albums" => packet.Items.Count > 0,
            "current-members" => packet.Facts.Any(f => f.Category is "Obecny skład" or "Dawny skład"),
            _ => packet.Facts.Count > 0 || packet.Genres.Count > 0
        };
        return packet;
    }

    public async Task<AiIdentityContext?> GetIdentityAsync(string itemType, string spotifyItemId)
    {
        var subject = await ResolveSubjectAsync(itemType, spotifyItemId);
        if (subject == null) return null;
        return new AiIdentityContext
        {
            ItemType = itemType.Trim().ToLowerInvariant(),
            SubjectName = subject.Name,
            ArtistName = subject.ArtistName,
            AlbumName = subject.AlbumName
        };
    }

    private async Task<ResearchSubject?> ResolveSubjectAsync(string itemType, string id)
    {
        var normalized = itemType.Trim().ToLowerInvariant();
        if (normalized == "artist")
        {
            var artist = await context.SpArtists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return artist == null ? null : new ResearchSubject(artist.Name, artist.Name, null, false);
        }
        if (normalized == "album")
        {
            var album = await context.SpAlbums.AsNoTracking().Include(x => x.AlbumArtistAssigns).ThenInclude(x => x.Artist).FirstOrDefaultAsync(x => x.Id == id);
            return album == null ? null : new ResearchSubject(album.Name, album.AlbumArtistAssigns.FirstOrDefault()?.Artist?.Name, album.Name, true);
        }
        if (normalized == "track")
        {
            var track = await context.SpTracks.AsNoTracking()
                .Include(x => x.TrackArtists).ThenInclude(x => x.Artist)
                .Include(x => x.Album).ThenInclude(x => x!.AlbumArtistAssigns).ThenInclude(x => x.Artist)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (track == null) return null;
            var artist = track.TrackArtists.FirstOrDefault()?.Artist?.Name ?? track.Album?.AlbumArtistAssigns.FirstOrDefault()?.Artist?.Name;
            return new ResearchSubject(track.Name, artist, track.Album?.Name, false);
        }
        return null;
    }

    private async Task<MusicBrainzEntity?> FindMusicBrainzEntityAsync(string type, string artist, string? album, CancellationToken ct)
    {
        var query = type == "artist" ? $"artist:\"{artist}\"" : $"releasegroup:\"{album}\" AND artist:\"{artist}\"";
        var root = await GetMusicBrainzAsync($"{type}?query={Uri.EscapeDataString(query)}&fmt=json&limit=8", ct);
        var candidates = root?[$"{type}s"]?.AsArray() ?? [];
        var candidate = candidates.FirstOrDefault(item => CandidateMatches(item, type, artist, album));
        var id = GetString(candidate, "id");
        return string.IsNullOrWhiteSpace(id) ? null : new MusicBrainzEntity(type, id);
    }

    private static bool CandidateMatches(JsonNode? candidate, string type, string artist, string? album)
    {
        if (candidate == null) return false;
        return type == "artist"
            ? NamesMatch(GetString(candidate, "name"), artist)
            : NamesMatch(GetString(candidate, "title"), album) && NamesMatch(GetArtistCredit(candidate), artist);
    }

    private async Task AddArtistResearchAsync(AiResearchPacket packet, MusicBrainzEntity entity, CancellationToken ct)
    {
        var node = await GetMusicBrainzAsync($"artist/{entity.Id}?inc=genres+tags+artist-rels+url-rels&fmt=json", ct);
        if (node == null)
        {
            packet.Warnings.Add("MusicBrainz nie odpowiedział danymi o wybranym artyście.");
            return;
        }

        var source = new AiSourceLink { Name = "MusicBrainz — artysta", Url = $"https://musicbrainz.org/artist/{entity.Id}", Detail = "Profil artysty, relacje i gatunki" };
        AddSource(packet, source);
        AddGenres(packet, node);
        AddFact(packet, "Profil", "Typ", GetString(node, "type"), source);
        AddFact(packet, "Historia", "Początek działalności", GetString(node?["life-span"], "begin"), source);
        AddFact(packet, "Historia", "Koniec działalności", GetString(node?["life-span"], "end"), source);
        AddFact(packet, "Pochodzenie", "Miejsce rozpoczęcia działalności", GetString(node?["begin-area"], "name"), source);
        AddFact(packet, "Pochodzenie", "Obszar działalności", GetString(node?["area"], "name"), source);

        if (string.Equals(GetString(node, "type"), "Group", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var relation in node?["relations"]?.AsArray() ?? [])
            {
                if (!string.Equals(GetString(relation, "type"), "member of band", StringComparison.OrdinalIgnoreCase)) continue;
                var name = GetString(relation?["artist"], "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var begin = GetString(relation, "begin");
                var end = GetString(relation, "end");
                var duration = string.IsNullOrWhiteSpace(end)
                    ? string.IsNullOrWhiteSpace(begin) ? "brak dat w MusicBrainz" : $"od {begin}; brak daty zakończenia w MusicBrainz"
                    : string.IsNullOrWhiteSpace(begin) ? $"do {end}" : $"{begin}–{end}";
                AddFact(packet, string.IsNullOrWhiteSpace(end) ? "Obecny skład" : "Dawny skład", name, duration, source);
            }
        }

        var wikidataId = ExtractWikidataId(node) ?? await FindWikidataIdAsync(entity.Id, "P434", ct);
        if (!string.IsNullOrWhiteSpace(wikidataId)) await AddWikidataResearchAsync(packet, wikidataId, ct);
    }

    private async Task AddAlbumResearchAsync(AiResearchPacket packet, MusicBrainzEntity entity, CancellationToken ct)
    {
        var node = await GetMusicBrainzAsync($"release-group/{entity.Id}?inc=genres+tags+artist-credits+url-rels&fmt=json", ct);
        if (node == null)
        {
            packet.Warnings.Add("MusicBrainz nie odpowiedział danymi o wybranym albumie.");
            return;
        }

        var source = new AiSourceLink { Name = "MusicBrainz — album", Url = $"https://musicbrainz.org/release-group/{entity.Id}", Detail = "Profil release group, data pierwszego wydania i gatunki" };
        AddSource(packet, source);
        AddGenres(packet, node);
        AddFact(packet, "Wydanie", "Pierwsze wydanie", GetString(node, "first-release-date"), source);
        AddFact(packet, "Wydanie", "Typ", GetString(node, "primary-type"), source);
        var secondary = node?["secondary-types"]?.AsArray().Select(item => item?.GetValue<string>()).Where(item => !string.IsNullOrWhiteSpace(item));
        AddFact(packet, "Wydanie", "Dodatkowy typ", secondary == null ? null : string.Join(", ", secondary!), source);
        AddFact(packet, "Wydanie", "Wykonawca", GetArtistCredit(node), source);

        var wikidataId = ExtractWikidataId(node) ?? await FindWikidataIdAsync(entity.Id, "P436", ct);
        if (!string.IsNullOrWhiteSpace(wikidataId)) await AddWikidataResearchAsync(packet, wikidataId, ct);
    }

    private async Task AddWikidataResearchAsync(AiResearchPacket packet, string id, CancellationToken ct)
    {
        var root = await GetWikidataAsync($"wiki/Special:EntityData/{id}.json", ct);
        var node = root?["entities"]?[id];
        if (node == null) return;

        var source = new AiSourceLink { Name = "Wikidata", Url = $"https://www.wikidata.org/wiki/{id}", Detail = "Dane encyklopedyczne Wikidata" };
        AddSource(packet, source);
        AddFact(packet, "Opis", "Opis", GetLocalizedWikidataValue(node?["descriptions"]), source);
        AddFact(packet, "Historia", "Data powstania", GetWikidataTime(node, "P571"), source);
        AddFact(packet, "Historia", "Data rozwiązania", GetWikidataTime(node, "P576"), source);
        AddFact(packet, "Link", "Oficjalna strona", GetWikidataString(node, "P856"), source);
    }

    private async Task AddLastFmResearchAsync(AiResearchPacket packet, string intent, string artist, CancellationToken ct)
    {
        var result = intent switch
        {
            "artist-top-albums" => await GetLastFmTopAlbumsAsync(artist, ct),
            "similar-artists" => await GetLastFmSimilarArtistsAsync(artist, ct),
            _ => await GetSimilarAlbumsAsync(artist, ct)
        };
        packet.Items.AddRange(result.Items);
        if (result.Items.Count > 0)
        {
            AddSource(packet, new AiSourceLink
            {
                Name = "Last.fm",
                Url = result.SourceUrl ?? "https://www.last.fm/",
                Detail = intent == "artist-top-albums" ? "Ranking popularności (liczba odsłuchań)" : "Społecznościowe podobieństwo"
            });
        }
        else
        {
            packet.Warnings.Add(result.Status switch
            {
                LastFmStatus.NoApiKey => "Brak klucza Last.fm w ustawieniach AI, więc nie można pobrać podobieństw ani rankingu popularności.",
                LastFmStatus.Error => "Last.fm nie zwrócił poprawnych danych dla tego pytania.",
                _ => "Last.fm nie znalazł wyników dla tego pytania."
            });
        }
    }

    private async Task<LastFmItemsResult> GetLastFmSimilarArtistsAsync(string artist, CancellationToken ct)
    {
        var lookup = await GetLastFmAsync("artist.getsimilar", artist, ct);
        var values = lookup.Data?["similarartists"]?["artist"] as JsonArray;
        var items = values?.Select(item => new AiResultItem
        {
            Name = GetString(item, "name") ?? string.Empty,
            Score = Decimal.TryParse(GetString(item, "match"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var score) ? score : null,
            Metric = "podobieństwo Last.fm",
            Url = GetString(item, "url")
        }).Where(item => !string.IsNullOrWhiteSpace(item.Name)).Take(8).ToList() ?? [];
        return new LastFmItemsResult(items, items.Count > 0 ? LastFmStatus.Success : lookup.Status, GetLastFmArtistUrl(artist));
    }

    private async Task<LastFmItemsResult> GetLastFmTopAlbumsAsync(string artist, CancellationToken ct)
    {
        var lookup = await GetLastFmAsync("artist.gettopalbums", artist, ct);
        var values = lookup.Data?["topalbums"]?["album"] as JsonArray;
        var items = values?.Select(item => new AiResultItem
        {
            Name = GetString(item, "name") ?? string.Empty,
            Score = Decimal.TryParse(GetString(item, "playcount"), out var plays) ? plays : null,
            Metric = "popularność Last.fm (liczba odsłuchań)",
            Url = GetString(item, "url")
        }).Where(item => !string.IsNullOrWhiteSpace(item.Name)).Take(8).ToList() ?? [];
        return new LastFmItemsResult(items, items.Count > 0 ? LastFmStatus.Success : lookup.Status, GetLastFmArtistUrl(artist));
    }

    private async Task<LastFmItemsResult> GetSimilarAlbumsAsync(string artist, CancellationToken ct)
    {
        var similar = await GetLastFmSimilarArtistsAsync(artist, ct);
        if (similar.Items.Count == 0) return similar;
        var results = new List<AiResultItem>();
        foreach (var candidate in similar.Items.Take(5))
        {
            var albums = await GetLastFmTopAlbumsAsync(candidate.Name, ct);
            results.AddRange(albums.Items.Take(1).Select(album => new AiResultItem
            {
                Name = album.Name,
                Subtitle = candidate.Name,
                Score = candidate.Score,
                Metric = "podobieństwo wykonawcy Last.fm",
                Url = album.Url
            }));
        }
        return new LastFmItemsResult(results.OrderByDescending(item => item.Score).ToList(), results.Count > 0 ? LastFmStatus.Success : LastFmStatus.NoResults, similar.SourceUrl);
    }

    private async Task<LastFmLookup> GetLastFmAsync(string method, string artist, CancellationToken ct)
    {
        var key = await settingsService.GetLastFmApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key)) return new LastFmLookup(null, LastFmStatus.NoApiKey);
        try
        {
            var uri = $"?method={method}&artist={Uri.EscapeDataString(artist)}&api_key={Uri.EscapeDataString(key)}&format=json&limit=8";
            var root = await GetCachedJsonAsync("lastfm", method + ":" + artist, TimeSpan.FromDays(7), async () =>
            {
                var client = httpClientFactory.CreateClient("LastFm");
                using var response = await client.GetAsync(uri, ct);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
            }, node => node["error"] == null);
            return root == null ? new LastFmLookup(null, LastFmStatus.Error) : new LastFmLookup(root, LastFmStatus.Success);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return new LastFmLookup(null, LastFmStatus.Error); }
    }

    private async Task<JsonNode?> GetMusicBrainzAsync(string uri, CancellationToken ct)
    {
        try
        {
            return await GetCachedJsonAsync("musicbrainz", uri, TimeSpan.FromDays(30), async () =>
            {
                await MusicBrainzLock.WaitAsync(ct);
                try
                {
                    var wait = TimeSpan.FromSeconds(1) - (DateTime.UtcNow - _lastMusicBrainzCallUtc);
                    if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
                    var client = httpClientFactory.CreateClient("MusicBrainz");
                    using var response = await client.GetAsync(uri, ct);
                    _lastMusicBrainzCallUtc = DateTime.UtcNow;
                    return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
                }
                finally { MusicBrainzLock.Release(); }
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return null; }
    }

    private async Task<JsonNode?> GetWikidataAsync(string uri, CancellationToken ct)
    {
        try
        {
            return await GetCachedJsonAsync("wikidata", uri, TimeSpan.FromDays(30), async () =>
            {
                var client = httpClientFactory.CreateClient("Wikidata");
                using var response = await client.GetAsync(uri, ct);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return null; }
    }

    private async Task<string?> FindWikidataIdAsync(string musicBrainzId, string property, CancellationToken ct)
    {
        var query = $"SELECT ?item WHERE {{ ?item wdt:{property} \"{musicBrainzId}\". }} LIMIT 2";
        var uri = "sparql?format=json&query=" + Uri.EscapeDataString(query);
        try
        {
            var root = await GetCachedJsonAsync("wikidata-query", property + ":" + musicBrainzId, TimeSpan.FromDays(30), async () =>
            {
                var client = httpClientFactory.CreateClient("WikidataQuery");
                using var response = await client.GetAsync(uri, ct);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
            });
            var values = root?["results"]?["bindings"]?.AsArray().Select(item => GetString(item?["item"], "value")).Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [];
            return values.Count == 1 ? values[0]!.Split('/').Last() : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return null; }
    }

    private async Task<JsonNode?> GetCachedJsonAsync(string source, string key, TimeSpan lifetime, Func<Task<string?>> get, Func<JsonNode, bool>? valid = null)
    {
        var cached = await context.AiExternalCacheEntries.FirstOrDefaultAsync(entry => entry.Source == source && entry.CacheKey == key);
        var cachedNode = TryParse(cached?.PayloadJson);
        if (cachedNode != null && (valid?.Invoke(cachedNode) ?? true) && cached!.ExpiresAtUtc > DateTime.UtcNow) return cachedNode;

        if (cached != null && (cachedNode == null || !(valid?.Invoke(cachedNode) ?? true)))
        {
            context.AiExternalCacheEntries.Remove(cached);
            cached = null;
        }
        var payload = await get();
        var node = TryParse(payload);
        if (node == null || !(valid?.Invoke(node) ?? true))
        {
            if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();
            return cachedNode != null && (valid?.Invoke(cachedNode) ?? true) ? cachedNode : null;
        }
        if (cached == null)
        {
            cached = new AiExternalCacheEntry { Source = source, CacheKey = key };
            context.AiExternalCacheEntries.Add(cached);
        }
        cached.PayloadJson = payload!;
        cached.UpdatedAtUtc = DateTime.UtcNow;
        cached.ExpiresAtUtc = DateTime.UtcNow.Add(lifetime);
        await context.SaveChangesAsync();
        return node;
    }

    private static void AddGenres(AiResearchPacket packet, JsonNode node)
    {
        foreach (var key in new[] { "genres", "tags" })
        {
            if (node[key] is not JsonArray values) continue;
            packet.Genres.AddRange(values.Select(item => GetString(item, "name")).Where(value => !string.IsNullOrWhiteSpace(value))!);
        }
        packet.Genres = packet.Genres.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
    }

    private static void AddFact(AiResearchPacket packet, string category, string label, string? value, AiSourceLink source)
    {
        if (string.IsNullOrWhiteSpace(value) || packet.Facts.Any(f => f.Label == label && f.Value == value && f.SourceUrl == source.Url)) return;
        packet.Facts.Add(new AiResearchFact { Category = category, Label = label, Value = value, SourceName = source.Name, SourceUrl = source.Url });
    }

    private static void AddSource(AiResearchPacket packet, AiSourceLink source)
    {
        if (!packet.Sources.Any(item => string.Equals(item.Url, source.Url, StringComparison.OrdinalIgnoreCase))) packet.Sources.Add(source);
    }

    private static string? ExtractWikidataId(JsonNode? node)
    {
        foreach (var relation in node?["relations"]?.AsArray() ?? [])
        {
            var id = ExtractWikidataId(GetString(relation?["url"], "resource"));
            if (id != null) return id;
        }
        return null;
    }

    private static string? ExtractWikidataId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains("wikidata.org", StringComparison.OrdinalIgnoreCase)) return null;
        var id = value.TrimEnd('/').Split('/').Last();
        return id.Length > 1 && id[0] == 'Q' && id[1..].All(char.IsDigit) ? id : null;
    }

    private static string? GetLocalizedWikidataValue(JsonNode? node) => GetString(node?["pl"], "value") ?? GetString(node?["en"], "value");
    private static string? GetWikidataString(JsonNode? node, string property) => GetString(node?["claims"]?[property]?.AsArray()?.FirstOrDefault()?["mainsnak"]?["datavalue"], "value");
    private static string? GetWikidataTime(JsonNode? node, string property)
    {
        var value = GetWikidataString(node, property);
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.TrimStart('+', '-');
        return trimmed.Length >= 4 && trimmed[..4].All(char.IsDigit) ? value.StartsWith('-') ? "−" + trimmed[..4] : trimmed[..4] : null;
    }

    private static string? GetArtistCredit(JsonNode? node)
    {
        var credits = node?["artist-credit"]?.AsArray();
        return credits == null ? null : string.Join(", ", credits.Select(credit => GetString(credit, "name") ?? GetString(credit?["artist"], "name")).Where(name => !string.IsNullOrWhiteSpace(name))!);
    }

    private static string? GetString(JsonNode? node, string key)
    {
        try { return node?[key]?.GetValue<string>(); }
        catch { return null; }
    }
    private static JsonNode? TryParse(string? value) { try { return string.IsNullOrWhiteSpace(value) ? null : JsonNode.Parse(value); } catch { return null; } }
    private static bool NamesMatch(string? actual, string? expected) => !string.IsNullOrWhiteSpace(actual) && !string.IsNullOrWhiteSpace(expected) && NormalizeName(actual) == NormalizeName(expected);
    private static string NormalizeName(string value) => new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    private static string GetLastFmArtistUrl(string artist) => "https://www.last.fm/music/" + Uri.EscapeDataString(artist).Replace("%20", "+", StringComparison.OrdinalIgnoreCase);

    private sealed record ResearchSubject(string Name, string? ArtistName, string? AlbumName, bool IsAlbum);
    private sealed record MusicBrainzEntity(string Type, string Id);
    private sealed record LastFmLookup(JsonNode? Data, LastFmStatus Status);
    private sealed record LastFmItemsResult(List<AiResultItem> Items, LastFmStatus Status, string? SourceUrl);
    private enum LastFmStatus { Success, NoApiKey, NoResults, Error }
}

public sealed class AiResearchPacket
{
    public string SubjectName { get; set; } = string.Empty;
    public string? ArtistName { get; set; }
    public string Intent { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public List<AiResearchFact> Facts { get; } = new();
    public List<AiResultItem> Items { get; } = new();
    public List<AiSourceLink> Sources { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool HasAnswerData { get; set; }
}

public sealed class AiIdentityContext
{
    public string ItemType { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
}
