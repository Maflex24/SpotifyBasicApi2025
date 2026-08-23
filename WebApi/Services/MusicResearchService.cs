using System.Text.Json;
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
        var genres = await GetMusicBrainzGenresAsync(subject.ArtistName ?? subject.Name, subject.IsAlbum ? subject.Name : null, cancellationToken);
        packet.Genres.AddRange(genres);
        packet.Sources.Add(new AiSourceLink { Name = "MusicBrainz", Url = "https://musicbrainz.org/", Detail = "Metadane i gatunki społeczności MusicBrainz" });

        if (intent is "artist-top-albums" or "similar-artists" or "similar-albums")
        {
            var artist = subject.ArtistName ?? subject.Name;
            if (intent == "artist-top-albums")
                packet.Items.AddRange(await GetLastFmTopAlbumsAsync(artist, cancellationToken));
            else if (intent == "similar-artists")
                packet.Items.AddRange(await GetLastFmSimilarArtistsAsync(artist, cancellationToken));
            else
                packet.Items.AddRange(await GetSimilarAlbumsAsync(artist, cancellationToken));

            packet.Sources.Add(new AiSourceLink { Name = "Last.fm", Url = "https://www.last.fm/", Detail = intent == "artist-top-albums" ? "Ranking popularności Last.fm" : "Społecznościowe podobieństwo Last.fm" });
        }
        return packet;
    }

    private async Task<ResearchSubject?> ResolveSubjectAsync(string itemType, string id)
    {
        var normalized = itemType.Trim().ToLowerInvariant();
        if (normalized == "artist")
        {
            var artist = await context.SpArtists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return artist == null ? null : new ResearchSubject(artist.Name, artist.Name, false);
        }
        if (normalized == "album")
        {
            var album = await context.SpAlbums.AsNoTracking().Include(x => x.AlbumArtistAssigns).ThenInclude(x => x.Artist).FirstOrDefaultAsync(x => x.Id == id);
            return album == null ? null : new ResearchSubject(album.Name, album.AlbumArtistAssigns.FirstOrDefault()?.Artist?.Name, true);
        }
        if (normalized == "track")
        {
            var track = await context.SpTracks.AsNoTracking().Include(x => x.TrackArtists).ThenInclude(x => x.Artist).FirstOrDefaultAsync(x => x.Id == id);
            return track == null ? null : new ResearchSubject(track.Name, track.TrackArtists.FirstOrDefault()?.Artist?.Name, false);
        }
        return null;
    }

    private async Task<List<string>> GetMusicBrainzGenresAsync(string artist, string? album, CancellationToken ct)
    {
        var entity = album == null ? "artist" : "release-group";
        var query = album == null ? $"artist:\"{artist}\"" : $"releasegroup:\"{album}\" AND artist:\"{artist}\"";
        var search = await GetMusicBrainzAsync($"{entity}?query={Uri.EscapeDataString(query)}&fmt=json&limit=1", ct);
        var first = search?[$"{entity}s"]?.AsArray().FirstOrDefault();
        var id = first?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id)) return new();

        var result = await GetMusicBrainzAsync($"{entity}/{id}?inc=genres+tags&fmt=json", ct);
        var values = new List<string>();
        foreach (var key in new[] { "genres", "tags" })
        {
            if (result?[key] is not JsonArray array) continue;
            values.AddRange(array.Select(x => x?["name"]?.GetValue<string>()).Where(x => !string.IsNullOrWhiteSpace(x))!);
        }
        return values.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
    }

    private async Task<List<AiResultItem>> GetLastFmSimilarArtistsAsync(string artist, CancellationToken ct)
    {
        var root = await GetLastFmAsync("artist.getsimilar", artist, ct);
        var artists = root?["similarartists"]?["artist"] as JsonArray;
        return artists?.Select(x => new AiResultItem
        {
            Name = x?["name"]?.GetValue<string>() ?? string.Empty,
            Score = Decimal.TryParse(x?["match"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var score) ? score : null,
            Metric = "podobieństwo Last.fm",
            Url = x?["url"]?.GetValue<string>()
        }).Where(x => !string.IsNullOrWhiteSpace(x.Name)).Take(8).ToList() ?? new();
    }

    private async Task<List<AiResultItem>> GetLastFmTopAlbumsAsync(string artist, CancellationToken ct)
    {
        var root = await GetLastFmAsync("artist.gettopalbums", artist, ct);
        var albums = root?["topalbums"]?["album"] as JsonArray;
        return albums?.Select(x => new AiResultItem
        {
            Name = x?["name"]?.GetValue<string>() ?? string.Empty,
            Score = Decimal.TryParse(x?["playcount"]?.ToString(), out var plays) ? plays : null,
            Metric = "popularność Last.fm (liczba odsłuchań)",
            Url = x?["url"]?.GetValue<string>()
        }).Where(x => !string.IsNullOrWhiteSpace(x.Name)).Take(8).ToList() ?? new();
    }

    private async Task<List<AiResultItem>> GetSimilarAlbumsAsync(string artist, CancellationToken ct)
    {
        var similar = await GetLastFmSimilarArtistsAsync(artist, ct);
        var results = new List<AiResultItem>();
        foreach (var candidate in similar.Take(5))
        {
            var albums = await GetLastFmTopAlbumsAsync(candidate.Name, ct);
            results.AddRange(albums.Take(1).Select(album => new AiResultItem
            {
                Name = album.Name,
                Subtitle = candidate.Name,
                Score = candidate.Score,
                Metric = "podobieństwo wykonawcy Last.fm",
                Url = album.Url
            }));
        }
        return results.OrderByDescending(x => x.Score).ToList();
    }

    private async Task<JsonNode?> GetLastFmAsync(string method, string artist, CancellationToken ct)
    {
        var key = await settingsService.GetLastFmApiKeyAsync();
        if (string.IsNullOrWhiteSpace(key)) return null;
        var uri = $"?method={method}&artist={Uri.EscapeDataString(artist)}&api_key={Uri.EscapeDataString(key)}&format=json&limit=8";
        return await GetCachedJsonAsync("lastfm", method + ":" + artist, async () =>
        {
            var client = httpClientFactory.CreateClient("LastFm");
            using var response = await client.GetAsync(uri, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
        });
    }

    private async Task<JsonNode?> GetMusicBrainzAsync(string relativeUri, CancellationToken ct)
    {
        return await GetCachedJsonAsync("musicbrainz", relativeUri, async () =>
        {
            await MusicBrainzLock.WaitAsync(ct);
            try
            {
                var wait = TimeSpan.FromSeconds(1) - (DateTime.UtcNow - _lastMusicBrainzCallUtc);
                if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
                var client = httpClientFactory.CreateClient("MusicBrainz");
                using var response = await client.GetAsync(relativeUri, ct);
                _lastMusicBrainzCallUtc = DateTime.UtcNow;
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
            }
            finally { MusicBrainzLock.Release(); }
        });
    }

    private async Task<JsonNode?> GetCachedJsonAsync(string source, string key, Func<Task<string?>> get)
    {
        var cached = await context.AiExternalCacheEntries.FirstOrDefaultAsync(x => x.Source == source && x.CacheKey == key);
        if (cached?.ExpiresAtUtc > DateTime.UtcNow)
            return JsonNode.Parse(cached.PayloadJson);
        var payload = await get();
        if (string.IsNullOrWhiteSpace(payload)) return cached == null ? null : JsonNode.Parse(cached.PayloadJson);

        if (cached == null)
        {
            cached = new AiExternalCacheEntry { Source = source, CacheKey = key };
            context.AiExternalCacheEntries.Add(cached);
        }
        cached.PayloadJson = payload;
        cached.UpdatedAtUtc = DateTime.UtcNow;
        cached.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await context.SaveChangesAsync();
        return JsonNode.Parse(payload);
    }

    private sealed record ResearchSubject(string Name, string? ArtistName, bool IsAlbum);
}

public sealed class AiResearchPacket
{
    public string SubjectName { get; set; } = string.Empty;
    public string? ArtistName { get; set; }
    public string Intent { get; set; } = string.Empty;
    public List<string> Genres { get; } = new();
    public List<AiResultItem> Items { get; } = new();
    public List<AiSourceLink> Sources { get; } = new();
}
