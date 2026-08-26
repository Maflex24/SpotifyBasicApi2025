using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

/// <summary>Builds library responses from Spotify's current user endpoints, never from the catalog mirror.</summary>
public sealed class LiveLibraryService(DatabaseContext context, LibraryOwnerService ownerService, SpotifyApiService spotifyApiService)
{
    private async Task<(string Owner, Dictionary<string, List<(long Id, string Name, string Group, float Strength)>> Tags, Dictionary<string, string> Notes)> LocalAsync()
    {
        var owner = await ownerService.RequireCurrentSpotifyOwnerAsync();
        var tags = await context.LibTagAssigns.AsNoTracking().Where(x => x.UserId == owner)
            .Select(x => new { x.ItemType, x.SpotifyItemId, x.TagId, Name = x.Tag.Name, Group = x.Tag.TagGroup.Name, x.Strength }).ToListAsync();
        var notes = await context.LibItemNotes.AsNoTracking().Where(x => x.UserId == owner)
            .ToDictionaryAsync(x => x.ItemType + "\u001f" + x.SpotifyItemId, x => x.Content);
        return (owner, tags.GroupBy(x => x.ItemType + "\u001f" + x.SpotifyItemId).ToDictionary(g => g.Key, g => g.Select(x => (x.TagId, x.Name, x.Group, x.Strength)).ToList()), notes);
    }

    private static List<long> ParseIds(string? value) => string.IsNullOrWhiteSpace(value) ? new() : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).Distinct().ToList();
    private static bool MatchTags(List<(long Id, string Name, string Group, float Strength)> tags, List<long> include, string? includeMode, List<long> exclude, string? excludeMode) =>
        (include.Count == 0 || (string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase) ? include.All(id => tags.Any(t => t.Id == id)) : include.Any(id => tags.Any(t => t.Id == id))))
        && (exclude.Count == 0 || !(string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase) ? exclude.All(id => tags.Any(t => t.Id == id)) : exclude.Any(id => tags.Any(t => t.Id == id))));
    private static List<SpotifyImage> Images(IEnumerable<SpotifyImageObject> images) => images.Select(x => new SpotifyImage { Url = x.Url, Height = x.Height, Width = x.Width }).ToList();
    private static bool Contains(string? value, string? search) => string.IsNullOrWhiteSpace(search) || (!string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase));

    public async Task<object> GetArtistsAsync(string? sort, string? order, string? includeTags, string? includeMode, string? excludeTags, string? excludeMode, string? genre, string? search, long? sortTagId)
    {
        var (owner, tags, notes) = await LocalAsync();
        var included = ParseIds(includeTags); var excluded = ParseIds(excludeTags);
        var artists = await spotifyApiService.GetLiveFollowedArtistsAsync(owner);
        var result = artists.Select(artist =>
        {
            var ownTags = tags.GetValueOrDefault("artist\u001f" + artist.Id, new());
            return new ArtistModel { Id = artist.Id, Name = artist.Name, Type = artist.Type, Uri = artist.Uri, Href = artist.Href, Genres = artist.Genres, Popularity = artist.Popularity, Images = Images(artist.Images), IsInLibrary = true, TagStrength = sortTagId.HasValue ? ownTags.FirstOrDefault(t => t.Id == sortTagId).Strength : 0, Tags = ownTags.Select(t => new ArtistTagModel { TagId = t.Id, TagName = t.Name, TagGroupName = t.Group, Strength = t.Strength }).ToList(), Note = notes.GetValueOrDefault("artist\u001f" + artist.Id) };
        }).Where(x => MatchTags(x.Tags.Select(t => (t.TagId, t.TagName, t.TagGroupName, t.Strength)).ToList(), included, includeMode, excluded, excludeMode))
          .Where(x => string.IsNullOrWhiteSpace(genre) || x.Genres.Any(g => string.Equals(g, genre, StringComparison.OrdinalIgnoreCase)))
          .Where(x => Contains(x.Name, search) || x.Genres.Any(g => Contains(g, search)) || x.Tags.Any(t => Contains(t.TagName, search)) || Contains(x.Note, search));
        result = (sort ?? "name").ToLowerInvariant() switch
        {
            "popularity" => string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.Popularity) : result.OrderBy(x => x.Popularity),
            "weight" => string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.TagStrength) : result.OrderBy(x => x.TagStrength),
            _ => string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.Name) : result.OrderBy(x => x.Name)
        };
        return new { items = result.ToList() };
    }

    public async Task<object> GetAlbumsAsync(string? sort, string? order, string? includeTags, string? includeMode, string? excludeTags, string? excludeMode, string? search, bool includeArtistTags, int page, int pageSize)
    {
        var (owner, tags, notes) = await LocalAsync(); var included = ParseIds(includeTags); var excluded = ParseIds(excludeTags);
        var albums = await spotifyApiService.GetLiveSavedAlbumsAsync(owner);
        var result = albums.Select(saved =>
        {
            var album = saved.Album; var own = tags.GetValueOrDefault("album\u001f" + album.Id, new());
            var inherited = includeArtistTags ? album.Artists.SelectMany(a => tags.GetValueOrDefault("artist\u001f" + a.Id, new())).ToList() : new();
            var all = own.Concat(inherited).GroupBy(t => t.Id).Select(g => g.First()).ToList();
            return new AlbumLive { Id = album.Id, Name = album.Name, Uri = album.Uri, AlbumType = album.AlbumType, TotalTracks = album.TotalTracks, Popularity = album.Popularity, ReleaseDate = album.ReleaseDate, ReleaseYear = int.TryParse(album.ReleaseDate?.Take(4).ToString(), out var year) ? year : null, Images = Images(album.Images), Tags = own.Select(t => new AlbumTagModel { TagId = t.Id, TagName = t.Name, TagGroupName = t.Group, Strength = t.Strength }).ToList(), Note = notes.GetValueOrDefault("album\u001f" + album.Id), Artists = album.Artists.Select(a => new SimpleArtist { Id = a.Id, Name = a.Name, Tags = tags.GetValueOrDefault("artist\u001f" + a.Id, new()).Select(t => new ArtistTagModel { TagId = t.Id, TagName = t.Name, TagGroupName = t.Group, Strength = t.Strength }).ToList(), Note = notes.GetValueOrDefault("artist\u001f" + a.Id) }).ToList(), _allTags = all };
        }).Where(x => MatchTags(x._allTags, included, includeMode, excluded, excludeMode))
          .Where(x => Contains(x.Name, search) || x.Artists.Any(a => Contains(a.Name, search)) || x.Tags.Any(t => Contains(t.TagName, search)) || Contains(x.Note, search));
        var ordered = (sort ?? "name").ToLowerInvariant() == "year" ? (string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.ReleaseDate) : result.OrderBy(x => x.ReleaseDate)) : string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.Name) : result.OrderBy(x => x.Name);
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 500); var list = ordered.ToList();
        return new { total = list.Count, page, pageSize, items = list.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.ToDto()).ToList() };
    }

    public async Task<object> GetTracksAsync(string? sort, string? order, string? includeTags, string? includeMode, string? excludeTags, string? excludeMode, string? search, int page, int pageSize)
    {
        var (owner, tags, _) = await LocalAsync(); var included = ParseIds(includeTags); var excluded = ParseIds(excludeTags);
        var tracks = await spotifyApiService.GetLiveSavedTracksAsync(owner);
        var result = tracks.Select(saved =>
        {
            var track = saved.Track; var own = tags.GetValueOrDefault("track\u001f" + track.Id, new());
            return new TrackLive { Id = track.Id, Name = track.Name, Uri = track.Uri, DurationMs = track.DurationMs, TrackNumber = track.TrackNumber, AlbumId = track.Album?.Id, AlbumName = track.Album?.Name, ReleaseYear = int.TryParse(track.Album?.ReleaseDate?.Take(4).ToString(), out var year) ? year : null, Images = track.Album == null ? new() : Images(track.Album.Images), Artists = track.Artists.Select(a => new SimpleArtist { Id = a.Id, Name = a.Name }).ToList(), Tags = own.Select(t => new TrackTagModel { TagId = t.Id, TagName = t.Name, TagGroupName = t.Group, Strength = t.Strength }).ToList(), _allTags = own };
        }).Where(x => MatchTags(x._allTags, included, includeMode, excluded, excludeMode)).Where(x => Contains(x.Name, search) || Contains(x.AlbumName, search) || x.Artists.Any(a => Contains(a.Name, search)) || x.Tags.Any(t => Contains(t.TagName, search)));
        var ordered = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.Name) : result.OrderBy(x => x.Name); page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 500); var list = ordered.ToList();
        return new { total = list.Count, page, pageSize, items = list.Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.ToDto()).ToList() };
    }

    public async Task<object> GetPlaylistsAsync(string? sort, string? order, string? search, long? folderId, int page, int pageSize)
    {
        var owner = await ownerService.RequireCurrentSpotifyOwnerAsync();
        var folders = await context.LibPlaylistFolderAssigns.AsNoTracking().Where(x => x.UserId == owner)
            .ToDictionaryAsync(x => x.PlaylistId, x => x.FolderId);
        var playlists = await spotifyApiService.GetLivePlaylistsAsync(owner);
        var result = playlists.Select(playlist => new { playlist.Id, playlist.Name, TotalTracks = playlist.Tracks.Total, Images = Images(playlist.Images), FolderId = folders.GetValueOrDefault(playlist.Id) })
            .Where(x => !folderId.HasValue || x.FolderId == folderId)
            .Where(x => Contains(x.Name, search));
        result = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase) ? result.OrderByDescending(x => x.Name) : result.OrderBy(x => x.Name);
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 500); var list = result.ToList();
        return new { total = list.Count, page, pageSize, items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList() };
    }

    private sealed class AlbumLive : AlbumModel { public List<(long Id, string Name, string Group, float Strength)> _allTags { get; set; } = new(); public AlbumModel ToDto() => this; }
    private sealed class TrackLive : TrackModel { public List<(long Id, string Name, string Group, float Strength)> _allTags { get; set; } = new(); public TrackModel ToDto() => this; }
}
