using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;

using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;
using System.Text.RegularExpressions;

namespace SpotifyBasicApi2025.Services
{
    public class LibraryService
    {
        private readonly DatabaseContext context;
        private readonly UserService userService;
        private readonly SpotifyRequestService spotifyRequestService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly LibraryOwnerService ownerService;
        private readonly ItemSnapshotService snapshotService;

        public DbSet<LibSavedFilter> SavedFilters => context.Set<LibSavedFilter>();

        private static List<string> GetSearchTerms(string? search) =>
            string.IsNullOrWhiteSpace(search)
                ? new List<string>()
                : Regex.Matches(search, @"[\p{L}\p{N}]+")
                    .Select(match => match.Value.ToLowerInvariant())
                    .ToList();

        private static int? GetReleaseYear(string? releaseDate)
        {
            if (string.IsNullOrWhiteSpace(releaseDate))
                return null;

            if (DateTime.TryParse(releaseDate, out var date))
                return date.Year;

            return releaseDate.Length >= 4 && int.TryParse(releaseDate[..4], out var year)
                ? year
                : null;
        }

        private sealed class AlbumReadRow
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string? Uri { get; init; }
            public string AlbumType { get; init; } = string.Empty;
            public int TotalTracks { get; init; }
            public int Popularity { get; init; }
            public string? ReleaseDate { get; init; }
            public string? Images { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime UpdatedAt { get; init; }
            public List<AlbumArtistReadRow> Artists { get; init; } = new();
        }

        private sealed class AlbumArtistReadRow
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string? Genres { get; init; }
            public List<ArtistTagModel> Tags { get; init; } = new();
        }

        // Saved filters API
        public async Task<List<object>> GetSavedFiltersAsync(string scope, string? contextId = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var q = context.LibSavedFilters
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.Scope == scope);

            if (!string.IsNullOrWhiteSpace(contextId))
                q = q.Where(f => f.ContextId == contextId);

            var items = await q
                .OrderBy(f => f.Name)
                .Select(f => new { f.Id, f.Name, f.Scope, f.ContextId, f.PayloadJson })
                .ToListAsync();

            return items.Cast<object>().ToList();
        }

        public async Task<long> CreateSavedFilterAsync(string scope, string? contextId, string name, string payloadJson)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var entity = new LibSavedFilter
            {
                UserId = userId,
                Scope = scope,
                ContextId = contextId,
                Name = name,
                PayloadJson = payloadJson,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            context.LibSavedFilters.Add(entity);
            await context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task UpdateSavedFilterAsync(long id, string name, string payloadJson)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var entity = await context.LibSavedFilters.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (entity == null)
                throw new InvalidOperationException("Filtr nie istnieje");

            entity.Name = name;
            entity.PayloadJson = payloadJson;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task DeleteSavedFilterAsync(long id)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var entity = await context.LibSavedFilters.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (entity == null)
                return;

            context.LibSavedFilters.Remove(entity);
            await context.SaveChangesAsync();
        }

        public LibraryService(DatabaseContext context, UserService userService, SpotifyRequestService spotifyRequestService, SpotifyApiService spotifyApiService, LibraryOwnerService ownerService, ItemSnapshotService snapshotService)
        {
            this.context = context;
            this.userService = userService;
            this.spotifyRequestService = spotifyRequestService;
            this.spotifyApiService = spotifyApiService;
            this.ownerService = ownerService;
            this.snapshotService = snapshotService;
        }

        private static string NormalizeNoteItemType(string? itemType)
        {
            var normalized = itemType?.Trim().ToLowerInvariant();
            return normalized is "artist" or "album"
                ? normalized
                : throw new ArgumentException("Notatki są obecnie dostępne tylko dla artystów i albumów.");
        }

        private async Task<bool> IsItemInUserLibraryAsync(string userId, string itemType, string spotifyItemId) => itemType switch
        {
            "artist" => await context.LibArtistUserAssigns.AnyAsync(assign => assign.UserId == userId && assign.ArtistId == spotifyItemId),
            "album" => await context.LibAlbumUserAssigns.AnyAsync(assign => assign.UserId == userId && assign.AlbumId == spotifyItemId),
            _ => false
        };

        public async Task<ItemNoteModel?> GetItemNoteAsync(string itemType, string spotifyItemId)
        {
            var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();

            var normalizedType = NormalizeNoteItemType(itemType);
            if (string.IsNullOrWhiteSpace(spotifyItemId))
                throw new ArgumentException("Identyfikator elementu jest wymagany.");

            return await context.LibItemNotes
                .AsNoTracking()
                .Where(note => note.UserId == userId && note.ItemType == normalizedType && note.SpotifyItemId == spotifyItemId)
                .Select(note => new ItemNoteModel
                {
                    ItemType = note.ItemType,
                    SpotifyItemId = note.SpotifyItemId,
                    Content = note.Content,
                    UpdatedAtUtc = note.UpdatedAtUtc
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ItemNoteModel?> SetItemNoteAsync(string itemType, string spotifyItemId, string? content, LibItemSnapshotInput? snapshot)
        {
            var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();

            var normalizedType = NormalizeNoteItemType(itemType);
            if (string.IsNullOrWhiteSpace(spotifyItemId))
                throw new ArgumentException("Identyfikator elementu jest wymagany.");

            var normalizedContent = content?.Trim() ?? string.Empty;
            var existing = await context.LibItemNotes
                .FirstOrDefaultAsync(note => note.UserId == userId && note.ItemType == normalizedType && note.SpotifyItemId == spotifyItemId);

            if (string.IsNullOrWhiteSpace(normalizedContent))
            {
                if (existing != null)
                {
                    context.LibItemNotes.Remove(existing);
                    await context.SaveChangesAsync();
                    await snapshotService.DeleteIfOrphanedAsync(userId, normalizedType, spotifyItemId);
                    await context.SaveChangesAsync();
                }

                return null;
            }

            if (existing == null)
            {
                snapshot ??= await ResolveNoteSnapshotAsync(normalizedType, spotifyItemId);
                if (snapshot is null)
                    throw new ArgumentException("Nie udało się pobrać opisu archiwalnego elementu ze Spotify.");
                existing = new LibItemNote
                {
                    UserId = userId,
                    ItemType = normalizedType,
                    SpotifyItemId = spotifyItemId,
                    Content = normalizedContent,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                context.LibItemNotes.Add(existing);
            }
            else
            {
                existing.Content = normalizedContent;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            if (snapshot is not null)
                await snapshotService.UpsertAsync(userId, normalizedType, spotifyItemId, snapshot);
            await context.SaveChangesAsync();
            return new ItemNoteModel
            {
                ItemType = existing.ItemType,
                SpotifyItemId = existing.SpotifyItemId,
                Content = existing.Content,
                UpdatedAtUtc = existing.UpdatedAtUtc
            };
        }

        private async Task<LibItemSnapshotInput?> ResolveNoteSnapshotAsync(string itemType, string spotifyItemId)
        {
            if (itemType == "artist")
            {
                var artist = await spotifyApiService.GetArtist(spotifyItemId);
                return artist == null ? null : new LibItemSnapshotInput { Title = artist.Name, Artists = new List<string> { artist.Name }, SpotifyUri = artist.Uri };
            }

            var album = await spotifyApiService.GetAlbumAsync(spotifyItemId);
            return album == null ? null : new LibItemSnapshotInput { Title = album.Name, Artists = album.Artists.Select(x => x.Name).ToList(), SpotifyUri = album.Uri };
        }

        public async Task<object> GetUserArtistsAsync(
            string? sort = null,
            string? order = null,
            string? tags = null,
            long? sortTagId = null,
            string? genre = null,
            string? includeTags = null,
            string? includeMode = null,
            string? includeTagStrengths = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? excludeTagStrengths = null,
            string? search = null,
            bool includeExternal = false)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            if (includeExternal && (!string.IsNullOrWhiteSpace(tags)
                || !string.IsNullOrWhiteSpace(includeTags)
                || !string.IsNullOrWhiteSpace(excludeTags)
                || sortTagId.HasValue))
                throw new ArgumentException("External search cannot be combined with tag filters.");

            var query = context.LibArtistUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Artist)
                .AsQueryable();

            query = ApplyArtistTagFilters(query, userId, includeTags, includeMode, includeTagStrengths, excludeTags, excludeMode, excludeTagStrengths);

            // Wsteczna kompatybilność: Filtrowanie po tagach (ANY)
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagIds = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.Parse(id))
                    .ToList();

                if (tagIds.Count > 0)
                {
                    query = query.Where(artist => context.LibTagAssigns.Any(ass =>
                        ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
                }
            }

            // Nowe filtrowanie: include/exclude z AND/OR
            if (!string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase);

                    if (andMode)
                    {
                        // artysta musi mieć wszystkie wskazane tagi
                        foreach (var tid in tagIds)
                        {
                            var idLocal = tid;
                            query = query.Where(artist => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        // OR – artysta musi mieć dowolny z tagów
                        query = query.Where(artist => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(excludeTags))
            {
                var tagIds = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase);

                    if (andMode)
                    {
                        // wyklucz artystów, którzy mają wszystkie wskazane tagi (czyli AND w warunku wykluczenia)
                        query = query.Where(artist => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && ass.TagId == tid)));
                    }
                    else
                    {
                        // OR – wyklucz jeśli ma choć jeden z tagów
                        query = query.Where(artist => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            // Filtrowanie po gatunku
            if (!string.IsNullOrWhiteSpace(genre))
            {
                var g = genre.Trim();

                query = query.Where(a => a.Genres != null && (
                    EF.Functions.Like(a.Genres!, g) ||
                    EF.Functions.Like(a.Genres!, g + ",%") ||
                    EF.Functions.Like(a.Genres!, g + ", %") ||
                    EF.Functions.Like(a.Genres!, "%, " + g + ", %") ||
                    EF.Functions.Like(a.Genres!, "%," + g + ",%") ||
                    EF.Functions.Like(a.Genres!, "%, " + g) ||
                    EF.Functions.Like(a.Genres!, "%," + g)
                ));
            }

            foreach (var term in GetSearchTerms(search))
            {
                var termLocal = term;
                query = query.Where(artist =>
                    (artist.Name != null && EF.Functions.Like(artist.Name.ToLower(), "%" + termLocal + "%"))
                    || (artist.Genres != null && EF.Functions.Like(artist.Genres.ToLower(), "%" + termLocal + "%"))
                    || context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.Id && context.LibTags.Any(tag => tag.Id == assign.TagId && EF.Functions.Like(tag.Name.ToLower(), "%" + termLocal + "%")))
                    || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == artist.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%")));
            }

            // Sortowanie
            var sortKey = (sort ?? "name").ToLowerInvariant();
            var sortOrderDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            if (sortKey == "weight" && !string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var firstTagId = tagIds.First();

                    // Sortuj po sile wybranego tagu (brak = 0)
                    query = sortOrderDesc
                        ? query.OrderByDescending(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == a.Id && ass.TagId == firstTagId).Select(ass => ass.Strength).FirstOrDefault())
                        : query.OrderBy(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == a.Id && ass.TagId == firstTagId).Select(ass => ass.Strength).FirstOrDefault());
                }
            }
            else
            {
                query = sortKey switch
                {
                    "popularity" => sortOrderDesc ? query.OrderByDescending(x => x.Popularity) : query.OrderBy(x => x.Popularity),
                    _ => sortOrderDesc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                };
            }

            var artists = await query.ToListAsync();

            // Pobierz tagi dla wszystkich artystów
            var artistIds = artists.Select(a => a.Id).ToList();
            var artistTags = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "artist" && artistIds.Contains(ass.SpotifyItemId))
                .Include(ass => ass.Tag)
                .ThenInclude(t => t.TagGroup)
                .ToListAsync();

            // Grupuj tagi według artysty
            var tagsByArtist = artistTags
                .GroupBy(ass => ass.SpotifyItemId)
                .ToDictionary(g => g.Key, g => g.Select(ass => ass.MapToArtistTagModel(ass.Tag, ass.Tag.TagGroup)).ToList());

            var notesByArtist = await context.LibItemNotes
                .AsNoTracking()
                .Where(note => note.UserId == userId && note.ItemType == "artist" && artistIds.Contains(note.SpotifyItemId))
                .ToDictionaryAsync(note => note.SpotifyItemId, note => note.Content);

            // Mapuj artystów do modeli z tagami
            var artistModels = artists.Select(artist =>
            {
                var tags = tagsByArtist.GetValueOrDefault(artist.Id, new List<ArtistTagModel>());
                var model = artist.MapToArtistModel(tags);
                model.IsInLibrary = true;
                model.Note = notesByArtist.GetValueOrDefault(artist.Id);
                model.TagStrength = sortTagId.HasValue
                    ? tags.FirstOrDefault(tag => tag.TagId == sortTagId.Value)?.Strength ?? 0f
                    : 0f;
                return model;
            }).ToList();

            // Dodatkowo dołącz bieżącą siłę wybranego tagu (jeśli podano)
            if (includeExternal && !string.IsNullOrWhiteSpace(search))
            {
                var existingIds = artistModels.Select(artist => artist.Id).ToHashSet(StringComparer.Ordinal);
                var spotifyArtists = await spotifyApiService.SearchArtistsAsync(search);
                var externalArtists = spotifyArtists
                    .Where(artist => !string.IsNullOrWhiteSpace(artist.Id) && !existingIds.Contains(artist.Id))
                    .Where(artist => string.IsNullOrWhiteSpace(genre) || artist.Genres.Any(value => string.Equals(value, genre.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .Select(artist => new ArtistModel
                    {
                        Id = artist.Id,
                        Name = artist.Name,
                        Type = artist.Type,
                        Uri = artist.Uri,
                        Href = artist.Href,
                        Genres = artist.Genres,
                        Popularity = artist.Popularity,
                        Images = artist.Images.Select(image => new SpotifyImage { Url = image.Url, Height = image.Height, Width = image.Width }).ToList(),
                        IsInLibrary = false
                    })
                    .ToList();

                artistModels.AddRange(externalArtists);
                artistModels = sortKey switch
                {
                    "popularity" => sortOrderDesc
                        ? artistModels.OrderByDescending(artist => artist.Popularity).ThenBy(artist => artist.Name, StringComparer.OrdinalIgnoreCase).ToList()
                        : artistModels.OrderBy(artist => artist.Popularity).ThenBy(artist => artist.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                    _ => sortOrderDesc
                        ? artistModels.OrderByDescending(artist => artist.Name, StringComparer.OrdinalIgnoreCase).ToList()
                        : artistModels.OrderBy(artist => artist.Name, StringComparer.OrdinalIgnoreCase).ToList()
                };
            }

            if (sortTagId.HasValue)
            {
                var artistDtos = artistModels
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.Popularity,
                        a.Genres,
                        a.Images,
                        a.Note,
                        TagStrength = a.Tags.FirstOrDefault(t => t.TagId == sortTagId.Value)?.Strength ?? 0f,
                        Tags = a.Tags
                    })
                    .ToList();

                return new { total = artistDtos.Count, items = artistDtos };
            }

            return new { total = artistModels.Count, items = artistModels };
        }

        public async Task<object> GetUserArtistTagsAsync()
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var tags = await context.LibTags
                .Where(t => t.TagGroup.UserId == userId && t.ForArtist)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.TagGroupId })
                .ToListAsync();

            return tags;
        }

        public async Task<List<object>> GetArtistTagCountsAsync(
            string? tags = null,
            string? genre = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibArtistUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Artist)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagIds = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(long.Parse)
                    .ToList();

                if (tagIds.Count > 0)
                    query = query.Where(artist => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
            }

            if (!string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase);

                    if (andMode)
                    {
                        foreach (var tid in tagIds)
                        {
                            var idLocal = tid;
                            query = query.Where(artist => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        query = query.Where(artist => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(excludeTags))
            {
                var tagIds = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase);

                    if (andMode)
                        query = query.Where(artist => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && ass.TagId == tid)));
                    else
                        query = query.Where(artist => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == artist.Id && tagIds.Contains(ass.TagId)));
                }
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                var g = genre.Trim();

                query = query.Where(a => a.Genres != null && (
                    EF.Functions.Like(a.Genres!, g) ||
                    EF.Functions.Like(a.Genres!, g + ",%") ||
                    EF.Functions.Like(a.Genres!, g + ", %") ||
                    EF.Functions.Like(a.Genres!, "%, " + g + ", %") ||
                    EF.Functions.Like(a.Genres!, "%," + g + ",%") ||
                    EF.Functions.Like(a.Genres!, "%, " + g) ||
                    EF.Functions.Like(a.Genres!, "%," + g)
                ));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(term => term.ToLower())
                    .ToList();

                foreach (var term in terms)
                {
                    var termLocal = term;
                    query = query.Where(artist =>
                        (artist.Name != null && EF.Functions.Like(artist.Name.ToLower(), "%" + termLocal + "%"))
                        || (artist.Genres != null && EF.Functions.Like(artist.Genres.ToLower(), "%" + termLocal + "%"))
                        || context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.Id && context.LibTags.Any(tag => tag.Id == assign.TagId && EF.Functions.Like(tag.Name.ToLower(), "%" + termLocal + "%")))
                        || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == artist.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%")));
                }
            }

            var artistIds = await query.Select(a => a.Id).ToListAsync();

            var counts = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "artist" && artistIds.Contains(ass.SpotifyItemId))
                .GroupBy(ass => new { ass.TagId, ass.Tag.Name })
                .Select(g => new { id = g.Key.TagId, name = g.Key.Name, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .ToListAsync();

            return counts.Cast<object>().ToList();
        }

        public async Task<List<string>> GetArtistsLibraryGenres()
        {
            var genresValues = context.SpArtists
                .AsNoTracking()
                .Where(a => a.Genres != string.Empty && a.Genres != null)
                .Select(a => a.Genres);

            var genres = new List<string>();

            foreach (var genre in genresValues)
            {
                var values = genre!.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var value in values)
                {
                    var v = value.Trim();

                    if (!string.IsNullOrEmpty(v) && !genres.Contains(v))
                        genres.Add(v);
                }
            }

            genres = genres.Order().ToList();

            return genres;
        }

        public async Task<bool> RemoveArtistFromLibraryAsync(string artistId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var assign = await context.LibArtistUserAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.ArtistId == artistId);
            if (assign != null)
            {
                context.LibArtistUserAssigns.Remove(assign);
                await context.SaveChangesAsync();
            }

            // Odobserwuj artystę w Spotify, aby nie wrócił przy sync
            var endpoint = $"me/following?type=artist&ids={Uri.EscapeDataString(artistId)}";
            var ok = await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint);
            return ok;
        }

        public async Task<bool> AddArtistToLibraryAsync(string artistId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User is not logged in");

            var endpoint = $"me/following?type=artist&ids={Uri.EscapeDataString(artistId)}";
            if (!await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint))
                return false;

            try
            {
                var artist = await context.SpArtists.FirstOrDefaultAsync(a => a.Id == artistId);
                if (artist == null)
                {
                    var spotifyArtist = await spotifyApiService.GetAndSaveArtist(artistId);
                    if (spotifyArtist == null)
                        throw new InvalidOperationException("Could not load artist from Spotify");
                    artist = await context.SpArtists.FirstAsync(a => a.Id == artistId);
                }

                if (!await context.LibArtistUserAssigns.AnyAsync(a => a.UserId == userId && a.ArtistId == artistId))
                {
                    context.LibArtistUserAssigns.Add(new LibArtistUserAssign
                    {
                        UserId = userId,
                        ArtistId = artistId,
                        ArtistName = artist.Name,
                        DateCreatedUtc = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint);
                throw;
            }
        }

        public async Task<List<ArtistDiscoveryImportResult>> AddArtistsForAlbumDiscoveryAsync(IEnumerable<string>? artistIds)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var ids = (artistIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
                throw new ArgumentException("Wybierz co najmniej jednego wykonawcę.");
            if (ids.Count > 20)
                throw new ArgumentException("Jednorazowo można dodać maksymalnie 20 wykonawców.");

            var results = new List<ArtistDiscoveryImportResult>();
            foreach (var artistId in ids)
            {
                var result = new ArtistDiscoveryImportResult { ArtistId = artistId };
                try
                {
                    result.AddedToLibrary = await AddArtistToLibraryAsync(artistId);
                    if (!result.AddedToLibrary)
                    {
                        result.Error = "Nie udało się dodać wykonawcy do biblioteki Spotify.";
                        results.Add(result);
                        continue;
                    }

                    var albumIds = (await spotifyApiService.GetArtistAlbumsAsync(artistId, includeGroups: "album"))
                        .Select(album => album.Id)
                        .Where(albumId => !string.IsNullOrWhiteSpace(albumId))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    var existingAlbumIds = (await context.LibAlbumUserAssigns
                        .Where(assign => assign.UserId == userId && albumIds.Contains(assign.AlbumId))
                        .Select(assign => assign.AlbumId)
                        .ToListAsync())
                        .ToHashSet(StringComparer.Ordinal);

                    result.AlbumsAlreadyInLibrary = existingAlbumIds.Count;
                    foreach (var albumId in albumIds.Where(albumId => !existingAlbumIds.Contains(albumId)))
                    {
                        try
                        {
                            if (await AddAlbumSynchronouslyAsync(albumId))
                                result.AlbumsAdded++;
                            else
                                result.AlbumsFailed++;
                        }
                        catch
                        {
                            result.AlbumsFailed++;
                        }
                    }

                    if (result.AlbumsFailed > 0)
                        result.Error = $"Nie udało się dodać {result.AlbumsFailed} albumów.";
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                }

                results.Add(result);
            }

            return results;
        }

        private static List<long> ParseTagIds(string? value) => string.IsNullOrWhiteSpace(value)
            ? new List<long>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(long.Parse)
                .Distinct()
                .ToList();

        private sealed record TagStrengthRule(long TagId, string Operator, float Value);

        private static Dictionary<long, TagStrengthRule> ParseTagStrengthRules(string? value, IReadOnlyCollection<long> selectedTagIds, string parameterName)
        {
            var rules = new Dictionary<long, TagStrengthRule>();
            if (string.IsNullOrWhiteSpace(value)) return rules;

            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pieces = part.Split(':', StringSplitOptions.TrimEntries);
                if (pieces.Length != 3 || !long.TryParse(pieces[0], out var tagId)
                    || (pieces[1] is not "gte" and not "lte")
                    || !float.TryParse(pieces[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var strength)
                    || float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f || strength > 6f
                    || !selectedTagIds.Contains(tagId) || !rules.TryAdd(tagId, new TagStrengthRule(tagId, pieces[1], strength)))
                    throw new ArgumentException($"Nieprawidłowy parametr {parameterName}.");
            }
            return rules;
        }

        private static Expression<Func<T, bool>> Combine<T>(IEnumerable<Expression<Func<T, bool>>> predicates, bool and)
        {
            var parameter = Expression.Parameter(typeof(T), "item");
            Expression? body = null;
            foreach (var predicate in predicates)
            {
                var replaced = new ParameterReplaceVisitor(predicate.Parameters[0], parameter).Visit(predicate.Body)!;
                body = body is null ? replaced : and ? Expression.AndAlso(body, replaced) : Expression.OrElse(body, replaced);
            }
            return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(true), parameter);
        }

        private static IQueryable<T> ApplyTagFilter<T>(IQueryable<T> query, IReadOnlyCollection<long> tagIds, string? mode, bool include, Dictionary<long, TagStrengthRule> rules, Func<long, TagStrengthRule?, Expression<Func<T, bool>>> hasTag)
        {
            if (tagIds.Count == 0) return query;
            var predicate = Combine(tagIds.Select(id => hasTag(id, rules.GetValueOrDefault(id))), string.Equals(mode, "and", StringComparison.OrdinalIgnoreCase));
            if (!include) predicate = Expression.Lambda<Func<T, bool>>(Expression.Not(predicate.Body), predicate.Parameters);
            return query.Where(predicate);
        }

        private IQueryable<SpArtist> ApplyArtistTagFilters(IQueryable<SpArtist> query, string userId, string? includeTags, string? includeMode, string? includeTagStrengths, string? excludeTags, string? excludeMode, string? excludeTagStrengths)
        {
            var included = ParseTagIds(includeTags);
            var excluded = ParseTagIds(excludeTags);
            Expression<Func<SpArtist, bool>> Has(long tagId, TagStrengthRule? rule)
            {
                if (rule is { Operator: "gte" }) { var value = rule.Value; return artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.Id && assign.TagId == tagId && assign.Strength >= value); }
                if (rule is { Operator: "lte" }) { var value = rule.Value; return artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.Id && assign.TagId == tagId && assign.Strength <= value); }
                return artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.Id && assign.TagId == tagId);
            }
            query = ApplyTagFilter(query, included, includeMode, true, ParseTagStrengthRules(includeTagStrengths, included, "includeTagStrengths"), Has);
            return ApplyTagFilter(query, excluded, excludeMode, false, ParseTagStrengthRules(excludeTagStrengths, excluded, "excludeTagStrengths"), Has);
        }

        private IQueryable<SpTrack> ApplyTrackTagFilters(IQueryable<SpTrack> query, string userId, string? includeTags, string? includeMode, string? includeTagStrengths, string? excludeTags, string? excludeMode, string? excludeTagStrengths)
        {
            var included = ParseTagIds(includeTags);
            var excluded = ParseTagIds(excludeTags);
            Expression<Func<SpTrack, bool>> Has(long tagId, TagStrengthRule? rule)
            {
                if (rule is { Operator: "gte" }) { var value = rule.Value; return track => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "track" && assign.SpotifyItemId == track.Id && assign.TagId == tagId && assign.Strength >= value); }
                if (rule is { Operator: "lte" }) { var value = rule.Value; return track => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "track" && assign.SpotifyItemId == track.Id && assign.TagId == tagId && assign.Strength <= value); }
                return track => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "track" && assign.SpotifyItemId == track.Id && assign.TagId == tagId);
            }
            query = ApplyTagFilter(query, included, includeMode, true, ParseTagStrengthRules(includeTagStrengths, included, "includeTagStrengths"), Has);
            return ApplyTagFilter(query, excluded, excludeMode, false, ParseTagStrengthRules(excludeTagStrengths, excluded, "excludeTagStrengths"), Has);
        }

        private IQueryable<SpAlbum> ApplyAlbumTagStrengthFilters(IQueryable<SpAlbum> query, string userId, string? includeTags, string? includeMode, string? includeTagStrengths, string? excludeTags, string? excludeMode, string? excludeTagStrengths, bool includeArtistTags)
        {
            var included = ParseTagIds(includeTags);
            var excluded = ParseTagIds(excludeTags);
            Expression<Func<SpAlbum, bool>> Has(long tagId, TagStrengthRule? rule)
            {
                Expression<Func<SpAlbum, bool>> direct = rule switch
                {
                    { Operator: "gte" } => album => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && assign.TagId == tagId && assign.Strength >= rule.Value),
                    { Operator: "lte" } => album => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && assign.TagId == tagId && assign.Strength <= rule.Value),
                    _ => album => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && assign.TagId == tagId)
                };
                if (!includeArtistTags) return direct;
                var artist = rule switch
                {
                    { Operator: "gte" } => (Expression<Func<SpAlbum, bool>>)(album => album.AlbumArtistAssigns.Any(item => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == item.ArtistId && assign.TagId == tagId && assign.Strength >= rule.Value))),
                    { Operator: "lte" } => album => album.AlbumArtistAssigns.Any(item => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == item.ArtistId && assign.TagId == tagId && assign.Strength <= rule.Value)),
                    _ => album => album.AlbumArtistAssigns.Any(item => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == item.ArtistId && assign.TagId == tagId))
                };
                return Combine(new[] { direct, artist }, false);
            }
            query = ApplyTagFilter(query, included, includeMode, true, ParseTagStrengthRules(includeTagStrengths, included, "includeTagStrengths"), Has);
            return ApplyTagFilter(query, excluded, excludeMode, false, ParseTagStrengthRules(excludeTagStrengths, excluded, "excludeTagStrengths"), Has);
        }

        private sealed class ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
        }

        private static List<string> ParseGenres(string? value) => string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(genre => genre.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private IQueryable<string> GetAlbumIdsForGenre(string genre)
        {
            var value = genre.ToLowerInvariant();
            return context.SpAlbumArtistAssigns
                .Where(assign => assign.Artist.Genres != null &&
                    (EF.Functions.Like(assign.Artist.Genres.ToLower(), value)
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), value + ",%")
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), value + ", %")
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), "%, " + value + ", %")
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), "%," + value + ",%")
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), "%, " + value)
                    || EF.Functions.Like(assign.Artist.Genres.ToLower(), "%," + value)))
                .Select(assign => assign.AlbumId);
        }

        private IQueryable<SpAlbum> ApplyAlbumGenres(
            IQueryable<SpAlbum> query,
            string? includeGenres,
            string? excludeGenres)
        {
            var included = ParseGenres(includeGenres);
            if (included.Count > 0)
            {
                IQueryable<string>? matchingAlbumIds = null;
                foreach (var genre in included)
                {
                    var genreIds = GetAlbumIdsForGenre(genre);
                    matchingAlbumIds = matchingAlbumIds == null ? genreIds : matchingAlbumIds.Union(genreIds);
                }

                query = query.Where(album => matchingAlbumIds!.Contains(album.Id));
            }

            foreach (var genre in ParseGenres(excludeGenres))
            {
                var matchingAlbumIds = GetAlbumIdsForGenre(genre);
                query = query.Where(album => !matchingAlbumIds.Contains(album.Id));
            }

            return query;
        }

        private IQueryable<SpAlbum> ApplyAlbumTags(
            IQueryable<SpAlbum> query,
            string userId,
            string? includeTags,
            string? includeMode,
            string? excludeTags,
            string? excludeMode,
            bool includeArtistTags)
        {
            var included = ParseTagIds(includeTags);
            if (included.Count > 0)
            {
                if (string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var tagId in included)
                    {
                        var tagIdLocal = tagId;
                        query = query.Where(album =>
                            context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && assign.TagId == tagIdLocal)
                            || (includeArtistTags && album.AlbumArtistAssigns.Any(artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.ArtistId && assign.TagId == tagIdLocal))));
                    }
                }
                else
                {
                    query = query.Where(album =>
                        context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && included.Contains(assign.TagId))
                        || (includeArtistTags && album.AlbumArtistAssigns.Any(artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && included.Contains(assign.TagId) && assign.SpotifyItemId == artist.ArtistId))));
                }
            }

            var excluded = ParseTagIds(excludeTags);
            if (excluded.Count > 0)
            {
                if (string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(album => !excluded.All(tagId =>
                        context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && assign.TagId == tagId)
                        || (includeArtistTags && album.AlbumArtistAssigns.Any(artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.ArtistId && assign.TagId == tagId)))));
                }
                else
                {
                    query = query.Where(album => !(
                        context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "album" && assign.SpotifyItemId == album.Id && excluded.Contains(assign.TagId))
                        || (includeArtistTags && album.AlbumArtistAssigns.Any(artist => context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "artist" && assign.SpotifyItemId == artist.ArtistId && excluded.Contains(assign.TagId))))));
                }
            }

            return query;
        }

        public async Task<bool> RemoveArtistSynchronouslyAsync(string artistId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User is not logged in");

            var endpoint = $"me/following?type=artist&ids={Uri.EscapeDataString(artistId)}";
            if (!await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint))
                return false;

            try
            {
                var assign = await context.LibArtistUserAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.ArtistId == artistId);
                if (assign != null)
                {
                    context.LibArtistUserAssigns.Remove(assign);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint);
                throw;
            }
        }

        // Spotify library mutations no longer populate the catalog mirror.
        public Task<bool> FollowArtistLiveAsync(string artistId) => spotifyRequestService.SendAsync(HttpMethod.Put, $"me/following?type=artist&ids={Uri.EscapeDataString(artistId)}");
        public Task<bool> UnfollowArtistLiveAsync(string artistId) => spotifyRequestService.SendAsync(HttpMethod.Delete, $"me/following?type=artist&ids={Uri.EscapeDataString(artistId)}");
        public Task<bool> SaveAlbumLiveAsync(string albumId) => spotifyRequestService.SendAsync(HttpMethod.Put, $"me/albums?ids={Uri.EscapeDataString(albumId)}");
        public Task<bool> RemoveSavedAlbumLiveAsync(string albumId) => spotifyRequestService.SendAsync(HttpMethod.Delete, $"me/albums?ids={Uri.EscapeDataString(albumId)}");
        public Task<bool> SaveTrackLiveAsync(string trackId) => spotifyRequestService.SendAsync(HttpMethod.Put, $"me/tracks?ids={Uri.EscapeDataString(trackId)}");
        public Task<bool> RemoveSavedTrackLiveAsync(string trackId) => spotifyRequestService.SendAsync(HttpMethod.Delete, $"me/tracks?ids={Uri.EscapeDataString(trackId)}");

        public async Task<bool> RemoveAlbumSynchronouslyAsync(string albumId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User is not logged in");

            var endpoint = $"me/albums?ids={Uri.EscapeDataString(albumId)}";
            if (!await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint))
                return false;

            try
            {
                var assign = await context.LibAlbumUserAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.AlbumId == albumId);
                if (assign != null)
                {
                    context.LibAlbumUserAssigns.Remove(assign);
                    await context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint);
                throw;
            }
        }

        public async Task<bool> RemoveAlbumFromLibraryAsync(string albumId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var assign = await context.LibAlbumUserAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.AlbumId == albumId);
            if (assign != null)
            {
                context.LibAlbumUserAssigns.Remove(assign);
                await context.SaveChangesAsync();
            }

            // Usuń zapisany album w Spotify
            var endpoint = $"me/albums?ids={Uri.EscapeDataString(albumId)}";
            var ok = await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint);
            return ok;
        }

        public async Task<bool> AddAlbumToLibraryAsync(string albumId)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var already = await context.SpSavedAlbums.AnyAsync(a => a.UserId == userId && a.AlbumId == albumId);

            if (!already)
            {
                // Upewnij się, że album istnieje w tabeli SpAlbums
                var album = await context.SpAlbums.FirstOrDefaultAsync(a => a.Id == albumId);
                if (album == null)
                {
                    var spAlbum = await spotifyApiService.GetAlbumAsync(albumId);
                    if (spAlbum == null)
                        throw new InvalidOperationException("Nie udało się pobrać informacji o albumie ze Spotify");

                    album = await spotifyApiService.SaveAlbumFromSpotifyAsync(spAlbum);
                }

                // Zapisz jako zapisany album
                context.SpSavedAlbums.Add(new SpSavedAlbum
                {
                    UserId = userId,
                    AlbumId = albumId,
                    AddedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });

                // Dodaj przypis Album-User do naszej tabeli (dla spójności)
                if (!await context.LibAlbumUserAssigns.AnyAsync(a => a.UserId == userId && a.AlbumId == albumId))
                {
                    context.LibAlbumUserAssigns.Add(new LibAlbumUserAssign
                    {
                        UserId = userId,
                        AlbumId = albumId,
                        DateCreatedUtc = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }

            // Dodaj zapisany album w Spotify
            var endpoint = $"me/albums?ids={Uri.EscapeDataString(albumId)}";
            var ok = await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint);
            return ok;
        }

        public async Task<bool> AddAlbumSynchronouslyAsync(string albumId)
        {
            var endpoint = $"me/albums?ids={Uri.EscapeDataString(albumId)}";
            if (!await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint))
                return false;

            try
            {
                // The existing method also maintains SpSavedAlbums and LibAlbumUserAssigns.
                await AddAlbumToLibraryAsync(albumId);
                return true;
            }
            catch
            {
                await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint);
                throw;
            }
        }

        public async Task<List<object>> GetLibraryItemStatusesAsync(string itemType, IEnumerable<string>? spotifyItemIds)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User is not logged in");

            var normalizedType = itemType?.Trim().ToLowerInvariant();
            if (normalizedType is not ("album" or "artist"))
                throw new InvalidOperationException("Supported item types are album and artist");

            var ids = (spotifyItemIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var savedIds = normalizedType == "album"
                ? await context.LibAlbumUserAssigns.Where(a => a.UserId == userId && ids.Contains(a.AlbumId)).Select(a => a.AlbumId).ToListAsync()
                : await context.LibArtistUserAssigns.Where(a => a.UserId == userId && ids.Contains(a.ArtistId)).Select(a => a.ArtistId).ToListAsync();

            var tagRows = await context.LibTagAssigns
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.ItemType == normalizedType && ids.Contains(a.SpotifyItemId))
                .Select(a => new { a.SpotifyItemId, a.TagId, TagName = a.Tag.Name, TagGroupName = a.Tag.TagGroup.Name, a.Strength })
                .ToListAsync();

            var tagsById = tagRows.GroupBy(row => row.SpotifyItemId).ToDictionary(group => group.Key, group => group.Select(row => (object)new
            {
                row.TagId,
                row.TagName,
                row.TagGroupName,
                row.Strength
            }).ToList());

            var savedSet = savedIds.ToHashSet(StringComparer.Ordinal);
            return ids.Select(id => (object)new
            {
                SpotifyItemId = id,
                IsInLibrary = savedSet.Contains(id),
                Tags = tagsById.GetValueOrDefault(id, new List<object>())
            }).ToList();
        }

        public async Task<bool> RemoveTrackFromLibraryAsync(string trackId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var assign = await context.LibTrackUserAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.TrackId == trackId);
            if (assign != null)
            {
                context.LibTrackUserAssigns.Remove(assign);
                await context.SaveChangesAsync();
            }

            // Usuń zapisany utwór w Spotify
            var endpoint = $"me/tracks?ids={Uri.EscapeDataString(trackId)}";
            var ok = await spotifyRequestService.SendAsync(HttpMethod.Delete, endpoint);
            return ok;
        }

        public async Task<bool> IsTrackInLibraryAsync(string trackId)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var existsAssign = await context.LibTrackUserAssigns.AnyAsync(a => a.UserId == userId && a.TrackId == trackId);

            if (existsAssign)
                return true;

            var existsSaved = await context.SpSavedTracks.AnyAsync(st => st.UserId == userId && st.TrackId == trackId);

            return existsSaved;
        }

        public async Task<bool> AddTrackToLibraryAsync(string trackId)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var already = await context.LibTrackUserAssigns.AnyAsync(a => a.UserId == userId && a.TrackId == trackId);

            if (!already)
            {
                // Upewnij się, że utwór istnieje w tabeli SpTracks
                var track = await context.SpTracks.FirstOrDefaultAsync(t => t.Id == trackId);
                if (track == null)
                {
                    var spTrack = await spotifyApiService.GetTrackAsync(trackId);
                    if (spTrack == null)
                        throw new InvalidOperationException("Nie udało się pobrać informacji o utworze ze Spotify");

                    track = await spotifyApiService.SaveTrackFromSpotifyAsync(spTrack);
                }

                context.LibTrackUserAssigns.Add(new LibTrackUserAssign
                {
                    UserId = userId,
                    TrackId = trackId,
                    TrackName = track!.Name,
                    DateCreatedUtc = DateTime.UtcNow
                });

                // Zapisz także jako zapisany utwór, aby sync był spójny
                if (!await context.SpSavedTracks.AnyAsync(s => s.UserId == userId && s.TrackId == trackId))
                {
                    context.SpSavedTracks.Add(new SpSavedTrack
                    {
                        UserId = userId,
                        TrackId = trackId,
                        AddedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }

            // Dodaj do biblioteki w Spotify
            var endpoint = $"me/tracks?ids={Uri.EscapeDataString(trackId)}";
            var ok = await spotifyRequestService.SendAsync(HttpMethod.Put, endpoint);
            return ok;
        }

        public async Task<object> GetUserTracksAsync(
            string? sort = null,
            string? order = null,
            string? tags = null,
            long? sortTagId = null,
            string? artistId = null,
            string? albumId = null,
            int? year = null,
            string? includeTags = null,
            string? includeMode = null,
            string? includeTagStrengths = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? excludeTagStrengths = null,
            int page = 1,
            int pageSize = 100,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibTrackUserAssigns
                .Where(a => a.UserId == userId)
                .Include(a => a.Track)
                    .ThenInclude(t => t.Album)
                .Include(a => a.Track)
                    .ThenInclude(t => t.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .Select(a => a.Track)
                .AsQueryable();

            query = ApplyTrackTagFilters(query, userId, includeTags, includeMode, includeTagStrengths, excludeTags, excludeMode, excludeTagStrengths);

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var aid = artistId.Trim();
                query = query.Where(track => context.SpTrackArtists.Any(x => x.TrackId == track.Id && x.ArtistId == aid));
            }

            if (!string.IsNullOrWhiteSpace(albumId))
                query = query.Where(track => track.AlbumId == albumId);

            if (year.HasValue)
            {
                var y = year.Value;
                query = query.Where(t => t.Album != null && t.Album.ReleaseDate != null && t.Album.ReleaseDate.StartsWith(y.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagIds = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                    query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
            }

            if (!string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                    {
                        foreach (var tid in tagIds)
                        {
                            var idLocal = tid;
                            query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(excludeTags))
            {
                var tagIds = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                        query = query.Where(track => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == tid)));
                    else
                        query = query.Where(track => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                }
            }

            // Wyszukiwanie (AND po słowach kluczowych): nazwa utworu, nazwa albumu, artyści, nazwy tagów
            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLower())
                    .ToList();

                foreach (var term in terms)
                {
                    var tlocal = term;

                    query = query.Where(t =>
                        (t.Name != null && EF.Functions.Like(t.Name.ToLower(), "%" + tlocal + "%"))
                        || (t.Album != null && t.Album.Name != null && EF.Functions.Like(t.Album.Name.ToLower(), "%" + tlocal + "%"))
                        || context.SpTrackArtists.Any(ta => ta.TrackId == t.Id && ta.Artist.Name != null && EF.Functions.Like(ta.Artist.Name.ToLower(), "%" + tlocal + "%"))
                        || context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && context.LibTags.Any(tag => tag.Id == ass.TagId && EF.Functions.Like(tag.Name.ToLower(), "%" + tlocal + "%")))
                    );
                }
            }

            var sortKey = (sort ?? "name").ToLowerInvariant();
            var sortDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            if (sortKey == "weight" && !string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                {
                    var firstTagId = tagIds.First();

                    query = sortDesc
                    ? query.OrderByDescending(t => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && ass.TagId == firstTagId).Select(ass => ass.Strength).FirstOrDefault())
                    : query.OrderBy(t => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && ass.TagId == firstTagId).Select(ass => ass.Strength).FirstOrDefault());
                }
            }
            else
            {
                query = sortKey switch
                {
                    "year" => sortDesc ? query.OrderByDescending(x => x.Album!.ReleaseDate) : query.OrderBy(x => x.Album!.ReleaseDate),
                    "popularity" => sortDesc ? query.OrderByDescending(x => x.Popularity) : query.OrderBy(x => x.Popularity),
                    "album" => sortDesc
                        ? query.OrderByDescending(x => x.Album!.Name)
                        : query.OrderBy(x => x.Album!.Name),
                    "artists" => sortDesc
                        ? query.OrderByDescending(x => x.TrackArtists
                            .Select(ta => ta.Artist.Name)
                            .OrderBy(n => n)
                            .FirstOrDefault())
                        : query.OrderBy(x => x.TrackArtists
                            .Select(ta => ta.Artist.Name)
                            .OrderBy(n => n)
                            .FirstOrDefault()),
                    "tags" => sortDesc
                        ? query.OrderByDescending(x => context.LibTagAssigns
                            .Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == x.Id)
                            .Count())
                        : query.OrderBy(x => context.LibTagAssigns
                            .Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == x.Id)
                            .Count()),
                    _ => sortDesc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                };
            }

            // Liczba całkowita przed paginacją
            var total = await query.CountAsync();

            // Paginacja
            if (page <= 0)
                page = 1;

            if (pageSize <= 0)
                pageSize = 100;

            var pageItemsQuery = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var tracks = await pageItemsQuery.ToListAsync();

            var trackIds = tracks.Select(t => t.Id).ToList();
            var trackTags = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "track" && trackIds.Contains(ass.SpotifyItemId))
                .Include(ass => ass.Tag)
                .ThenInclude(t => t.TagGroup)
                .ToListAsync();

            var tagsByTrack = trackTags
                .GroupBy(ass => ass.SpotifyItemId)
                .ToDictionary(g => g.Key, g => g.Select(ass => ass.MapToTrackTagModel(ass.Tag, ass.Tag.TagGroup)).ToList());

            var dtos = tracks.Select(t =>
            {
                var tagsList = tagsByTrack.GetValueOrDefault(t.Id, new List<TrackTagModel>());
                return t.MapToTrackModel(tagsList);
            }).ToList();

            if (sortTagId.HasValue)
            {
                var list = dtos.Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Uri,
                    a.DurationMs,
                    a.Popularity,
                    a.AlbumId,
                    a.AlbumName,
                    a.ReleaseYear,
                    a.Images,
                    a.Artists,
                    TagStrength = a.Tags.FirstOrDefault(t => t.TagId == sortTagId.Value)?.Strength ?? 0f,
                    Tags = a.Tags
                }).ToList();

                return new { total, page, pageSize, items = list };
            }

            return new { total, page, pageSize, items = dtos };
        }

        public async Task<List<object>> GetTrackTagCountsAsync(
            string? tags = null,
            string? artistId = null,
            string? albumId = null,
            int? year = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibTrackUserAssigns
                .Where(a => a.UserId == userId)
                .Include(a => a.Track)
                    .ThenInclude(t => t.Album)
                .Include(a => a.Track)
                    .ThenInclude(t => t.TrackArtists)
                        .ThenInclude(ta => ta.Artist)
                .Select(a => a.Track)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var aid = artistId.Trim();
                query = query.Where(track => context.SpTrackArtists.Any(x => x.TrackId == track.Id && x.ArtistId == aid));
            }

            if (!string.IsNullOrWhiteSpace(albumId))
                query = query.Where(track => track.AlbumId == albumId);

            if (year.HasValue)
            {
                var y = year.Value;
                query = query.Where(t => t.Album != null && t.Album.ReleaseDate != null && t.Album.ReleaseDate.StartsWith(y.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagIds = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                    query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
            }

            if (!string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                    {
                        foreach (var tid in tagIds)
                        {
                            var idLocal = tid;
                            query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        query = query.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(excludeTags))
            {
                var tagIds = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                        query = query.Where(track => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == tid)));
                    else
                        query = query.Where(track => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLower())
                    .ToList();

                foreach (var term in terms)
                {
                    var tlocal = term;
                    query = query.Where(t =>
                        (t.Name != null && EF.Functions.Like(t.Name.ToLower(), "%" + tlocal + "%"))
                        || (t.Album != null && t.Album.Name != null && EF.Functions.Like(t.Album.Name.ToLower(), "%" + tlocal + "%"))
                        || context.SpTrackArtists.Any(ta => ta.TrackId == t.Id && ta.Artist.Name != null && EF.Functions.Like(ta.Artist.Name.ToLower(), "%" + tlocal + "%"))
                        || context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && context.LibTags.Any(tag => tag.Id == ass.TagId && EF.Functions.Like(tag.Name.ToLower(), "%" + tlocal + "%")))
                    );
                }
            }

            var trackIds = await query.Select(t => t.Id).ToListAsync();

            var counts = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "track" && trackIds.Contains(ass.SpotifyItemId))
                .GroupBy(ass => new { ass.TagId, ass.Tag.Name })
                .Select(g => new { id = g.Key.TagId, name = g.Key.Name, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .ToListAsync();

            return counts.Cast<object>().ToList();
        }

        public async Task<object> GetUserAlbumsAsync(
            string? sort = null,
            string? order = null,
            string? tags = null,
            long? sortTagId = null,
            string? artistId = null,
            string? includeArtists = null,
            string? excludeArtists = null,
            int? fromYear = null,
            int? toYear = null,
            string? includeTags = null,
            string? includeMode = null,
            string? includeTagStrengths = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? excludeTagStrengths = null,
            string? includeGenres = null,
            string? excludeGenres = null,
            bool includeArtistTags = false,
            string? search = null,
            bool random = false,
            int page = 1,
            int pageSize = 100)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Album)
                .AsQueryable();

            query = ApplyAlbumTagStrengthFilters(query, userId, includeTags, includeMode, includeTagStrengths, excludeTags, excludeMode, excludeTagStrengths, includeArtistTags);

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var aid = artistId.Trim();
                query = query.Where(album => context.SpAlbumArtistAssigns.Any(x => x.AlbumId == album.Id && x.ArtistId == aid));
            }

            var includeArtistIds = ParseSpotifyIds(includeArtists);
            if (includeArtistIds.Count > 0)
                query = query.Where(album => album.AlbumArtistAssigns.Any(assign => includeArtistIds.Contains(assign.ArtistId)));

            var excludeArtistIds = ParseSpotifyIds(excludeArtists);
            if (excludeArtistIds.Count > 0)
                query = query.Where(album => !album.AlbumArtistAssigns.Any(assign => excludeArtistIds.Contains(assign.ArtistId)));

            query = ApplyAlbumTags(query, userId, tags, "or", null, null, includeArtistTags);

            if (fromYear.HasValue)
                query = query.Where(album => album.Year >= fromYear);

            if (toYear.HasValue)
                query = query.Where(album => album.Year <= toYear);

            query = ApplyAlbumTags(query, userId, includeTags, includeMode, excludeTags, excludeMode, includeArtistTags);
            query = ApplyAlbumGenres(query, includeGenres, excludeGenres);

            foreach (var term in GetSearchTerms(search))
            {
                var termLocal = term;
                query = query.Where(album =>
                    (album.Name != null && EF.Functions.Like(album.Name.ToLower(), "%" + termLocal + "%"))
                    || EF.Functions.Like(album.Id.ToLower(), "%" + termLocal + "%")
                    || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "album" && note.SpotifyItemId == album.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%"))
                    || context.SpAlbumArtistAssigns.Any(assign =>
                        assign.AlbumId == album.Id
                        && ((assign.Artist.Name != null && EF.Functions.Like(assign.Artist.Name.ToLower(), "%" + termLocal + "%"))
                            || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == assign.ArtistId && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%")))));
            }

            if (random)
            {
                var randomTotalRows = await query.CountAsync();
                if (randomTotalRows == 0)
                    return new { randomSearch = (string?)null, randomAlbumId = (string?)null };

                var randomOffset = Random.Shared.Next(randomTotalRows);
                var randomAlbum = await query
                    .AsNoTracking()
                    .OrderBy(album => album.Id)
                    .Skip(randomOffset)
                    .Select(album => new { album.Id, album.Name })
                    .FirstOrDefaultAsync();

                if (randomAlbum == null)
                    return new { randomSearch = (string?)null, randomAlbumId = (string?)null };

                var artistNames = await context.SpAlbumArtistAssigns
                    .AsNoTracking()
                    .Where(assign => assign.AlbumId == randomAlbum.Id)
                    .OrderBy(assign => assign.ArtistName)
                    .Select(assign => assign.ArtistName)
                    .ToListAsync();

                var randomSearch = string.Join(" ", new[]
                    {
                        search,
                        string.Join(" ", artistNames),
                        randomAlbum.Name,
                        randomAlbum.Id
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

                return new { randomSearch, randomAlbumId = randomAlbum.Id };
            }

            var sortKey = (sort ?? "name").ToLowerInvariant();
            var sortOrderDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            var weightTagId = sortTagId;
            if (!weightTagId.HasValue && !string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();

                if (tagIds.Count > 0)
                    weightTagId = tagIds.First();
            }

            if (sortKey == "weight" && weightTagId.HasValue)
            {
                var tagId = weightTagId.Value;
                query = sortOrderDesc
                    ? query.OrderByDescending(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == a.Id && ass.TagId == tagId).Select(ass => ass.Strength).FirstOrDefault()).ThenBy(a => a.Id)
                    : query.OrderBy(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == a.Id && ass.TagId == tagId).Select(ass => ass.Strength).FirstOrDefault()).ThenBy(a => a.Id);
            }
            else
            {
                query = sortKey switch
                {
                    "year" => sortOrderDesc ? query.OrderByDescending(x => x.ReleaseDate).ThenBy(x => x.Id) : query.OrderBy(x => x.ReleaseDate).ThenBy(x => x.Id),
                    "popularity" => sortOrderDesc ? query.OrderByDescending(x => x.Popularity).ThenBy(x => x.Id) : query.OrderBy(x => x.Popularity).ThenBy(x => x.Id),
                    "artist" => sortOrderDesc
                        ? query.OrderByDescending(x => x.AlbumArtistAssigns
                            .Select(aaa => aaa.Artist.Name)
                            .OrderBy(n => n)
                            .FirstOrDefault()).ThenBy(x => x.Id)
                        : query.OrderBy(x => x.AlbumArtistAssigns
                            .Select(aaa => aaa.Artist.Name)
                            .OrderBy(n => n)
                            .FirstOrDefault()).ThenBy(x => x.Id),
                    _ => sortOrderDesc ? query.OrderByDescending(x => x.Name).ThenBy(x => x.Id) : query.OrderBy(x => x.Name).ThenBy(x => x.Id),
                };
            }

            // Dodatkowe zabezpieczenie przed przeładowaniem
            if (page <= 0)
                page = 1;

            if (pageSize <= 0)
                pageSize = 100;

            pageSize = Math.Min(pageSize, 500);
            var usesAlbumNamePagingIndex = sortKey == "name"
                && !sortTagId.HasValue
                && string.IsNullOrWhiteSpace(tags)
                && string.IsNullOrWhiteSpace(artistId)
                && includeArtistIds.Count == 0
                && excludeArtistIds.Count == 0
                && !fromYear.HasValue
                && !toYear.HasValue
                && string.IsNullOrWhiteSpace(includeTags)
                && string.IsNullOrWhiteSpace(excludeTags)
                && string.IsNullOrWhiteSpace(search);

            int totalRows;
            List<AlbumReadRow> albums;

            if (usesAlbumNamePagingIndex)
            {
                // AlbumName jest kopią nazwy zapisaną przy przypisaniu albumu do biblioteki.
                // Indeks pozwala wybrać 100 ID przed dołączeniem pełnych rekordów SpAlbums.
                var assignments = context.LibAlbumUserAssigns
                    .AsNoTracking()
                    .Where(assign => assign.UserId == userId);

                totalRows = await assignments.CountAsync();
                assignments = sortOrderDesc
                    ? assignments.OrderByDescending(assign => assign.AlbumName).ThenBy(assign => assign.AlbumId)
                    : assignments.OrderBy(assign => assign.AlbumName).ThenBy(assign => assign.AlbumId);

                albums = await assignments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(assign => new AlbumReadRow
                    {
                        Id = assign.AlbumId,
                        Name = assign.Album.Name,
                        Uri = assign.Album.Uri,
                        AlbumType = assign.Album.AlbumType,
                        TotalTracks = assign.Album.TotalTracks,
                        Popularity = assign.Album.Popularity,
                        ReleaseDate = assign.Album.ReleaseDate,
                        Images = assign.Album.Images,
                        CreatedAt = assign.Album.CreatedAt,
                        UpdatedAt = assign.Album.UpdatedAt,
                        Artists = assign.Album.AlbumArtistAssigns.Select(artistAssign => new AlbumArtistReadRow
                        {
                            Id = artistAssign.ArtistId,
                            Name = artistAssign.ArtistName,
                            Genres = artistAssign.Artist.Genres,
                            Tags = context.LibTagAssigns
                                .Where(tagAssign => tagAssign.UserId == userId && tagAssign.ItemType == "artist" && tagAssign.SpotifyItemId == artistAssign.ArtistId)
                                .Select(tagAssign => new ArtistTagModel
                                {
                                    TagId = tagAssign.TagId,
                                    TagName = tagAssign.Tag.Name,
                                    TagGroupName = tagAssign.Tag.TagGroup.Name,
                                    Strength = tagAssign.Strength
                                }).ToList()
                        }).ToList()
                    })
                    .ToListAsync();
            }
            else
            {
                totalRows = await query.CountAsync();
                albums = await query
                    .AsNoTracking()
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(album => new AlbumReadRow
                    {
                        Id = album.Id,
                        Name = album.Name,
                        Uri = album.Uri,
                        AlbumType = album.AlbumType,
                        TotalTracks = album.TotalTracks,
                        Popularity = album.Popularity,
                        ReleaseDate = album.ReleaseDate,
                        Images = album.Images,
                        CreatedAt = album.CreatedAt,
                        UpdatedAt = album.UpdatedAt,
                        Artists = album.AlbumArtistAssigns.Select(assign => new AlbumArtistReadRow
                        {
                            Id = assign.ArtistId,
                            Name = assign.ArtistName,
                            Genres = assign.Artist.Genres,
                            Tags = context.LibTagAssigns
                                .Where(tagAssign => tagAssign.UserId == userId && tagAssign.ItemType == "artist" && tagAssign.SpotifyItemId == assign.ArtistId)
                                .Select(tagAssign => new ArtistTagModel
                                {
                                    TagId = tagAssign.TagId,
                                    TagName = tagAssign.Tag.Name,
                                    TagGroupName = tagAssign.Tag.TagGroup.Name,
                                    Strength = tagAssign.Strength
                                }).ToList()
                        }).ToList()
                    })
                    .ToListAsync();
            }

            var albumIds = albums.Select(a => a.Id).ToList();
            var albumTags = await context.LibTagAssigns
                .AsNoTracking()
                .Where(ass => ass.UserId == userId && ass.ItemType == "album" && albumIds.Contains(ass.SpotifyItemId))
                .Select(ass => new
                {
                    ass.SpotifyItemId,
                    ass.TagId,
                    ass.Strength,
                    TagName = ass.Tag.Name,
                    TagGroupName = ass.Tag.TagGroup.Name
                })
                .ToListAsync();

            var tagsByAlbum = albumTags
                .GroupBy(ass => ass.SpotifyItemId)
                .ToDictionary(g => g.Key, g => g.Select(ass => new AlbumTagModel
                {
                    TagId = ass.TagId,
                    TagName = ass.TagName,
                    TagGroupName = ass.TagGroupName,
                    Strength = ass.Strength
                }).ToList());

            var notesByAlbum = await context.LibItemNotes
                .AsNoTracking()
                .Where(note => note.UserId == userId && note.ItemType == "album" && albumIds.Contains(note.SpotifyItemId))
                .ToDictionaryAsync(note => note.SpotifyItemId, note => note.Content);

            var artistIds = albums
                .SelectMany(album => album.Artists)
                .Select(artist => artist.Id)
                .Distinct()
                .ToList();
            var notesByAlbumArtist = await context.LibItemNotes
                .AsNoTracking()
                .Where(note => note.UserId == userId && note.ItemType == "artist" && artistIds.Contains(note.SpotifyItemId))
                .ToDictionaryAsync(note => note.SpotifyItemId, note => note.Content);

            var albumModels = albums.Select(album =>
            {
                var tagsList = tagsByAlbum.GetValueOrDefault(album.Id, new List<AlbumTagModel>());
                return new AlbumModel
                {
                    Id = album.Id,
                    Name = album.Name,
                    Uri = album.Uri,
                    AlbumType = album.AlbumType,
                    TotalTracks = album.TotalTracks,
                    Popularity = album.Popularity,
                    ReleaseDate = album.ReleaseDate,
                    ReleaseYear = GetReleaseYear(album.ReleaseDate),
                    Images = ImageMappingExtensions.ParseSpotifyImages(album.Images),
                    CreatedAt = album.CreatedAt,
                    UpdatedAt = album.UpdatedAt,
                    Note = notesByAlbum.GetValueOrDefault(album.Id),
                    Tags = tagsList,
                    Artists = album.Artists.Select(artist => new SimpleArtist
                    {
                        Id = artist.Id,
                        Name = artist.Name,
                        Genres = ParseGenres(artist.Genres),
                        Tags = artist.Tags,
                        Note = notesByAlbumArtist.GetValueOrDefault(artist.Id)
                    }).ToList()
                };
            }).ToList();

            if (sortTagId.HasValue)
            {
                var albumDtos = albumModels
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.Uri,
                        a.AlbumType,
                        a.ReleaseDate,
                        a.ReleaseYear,
                        a.TotalTracks,
                        a.Popularity,
                        a.Images,
                        a.Artists,
                        a.Note,
                        TagStrength = a.Tags.FirstOrDefault(t => t.TagId == sortTagId.Value)?.Strength ?? 0f,
                        Tags = a.Tags
                    })
                    .ToList();

                return new { TotalRows = totalRows, total = totalRows, page, pageSize, items = albumDtos };
            }

            return new { TotalRows = totalRows, total = totalRows, page, pageSize, items = albumModels };
        }

        public async Task<List<object>> GetAlbumTagCountsAsync(
            string? tags = null,
            string? artistId = null,
            string? includeArtists = null,
            string? excludeArtists = null,
            int? fromYear = null,
            int? toYear = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? includeGenres = null,
            string? excludeGenres = null,
            bool includeArtistTags = false,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Album)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var aid = artistId.Trim();
                query = query.Where(album => context.SpAlbumArtistAssigns.Any(x => x.AlbumId == album.Id && x.ArtistId == aid));
            }

            var includeArtistIds = ParseSpotifyIds(includeArtists);
            if (includeArtistIds.Count > 0)
                query = query.Where(album => album.AlbumArtistAssigns.Any(assign => includeArtistIds.Contains(assign.ArtistId)));

            var excludeArtistIds = ParseSpotifyIds(excludeArtists);
            if (excludeArtistIds.Count > 0)
                query = query.Where(album => !album.AlbumArtistAssigns.Any(assign => excludeArtistIds.Contains(assign.ArtistId)));

            if (fromYear.HasValue)
                query = query.Where(album => album.Year >= fromYear.Value);

            if (toYear.HasValue)
                query = query.Where(album => album.Year <= toYear.Value);

            query = ApplyAlbumTags(query, userId, tags, "or", null, null, includeArtistTags);

            query = ApplyAlbumTags(query, userId, includeTags, includeMode, excludeTags, excludeMode, includeArtistTags);
            query = ApplyAlbumGenres(query, includeGenres, excludeGenres);

            foreach (var term in GetSearchTerms(search))
            {
                var termLocal = term;
                query = query.Where(album =>
                    (album.Name != null && EF.Functions.Like(album.Name.ToLower(), "%" + termLocal + "%"))
                    || EF.Functions.Like(album.Id.ToLower(), "%" + termLocal + "%")
                    || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "album" && note.SpotifyItemId == album.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%"))
                    || context.SpAlbumArtistAssigns.Any(assign =>
                        assign.AlbumId == album.Id
                        && ((assign.Artist.Name != null && EF.Functions.Like(assign.Artist.Name.ToLower(), "%" + termLocal + "%"))
                            || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == assign.ArtistId && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%")))));
            }

            var albumIds = await query.Select(a => a.Id).ToListAsync();

            var countsQuery = context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "album" && albumIds.Contains(ass.SpotifyItemId))
                .Select(ass => new { AlbumId = ass.SpotifyItemId, ass.TagId, TagName = ass.Tag.Name });

            if (includeArtistTags)
            {
                var artistCounts = context.SpAlbumArtistAssigns
                    .Where(assign => albumIds.Contains(assign.AlbumId))
                    .Join(context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "artist"),
                        artistAssign => artistAssign.ArtistId,
                        tagAssign => tagAssign.SpotifyItemId,
                        (artistAssign, tagAssign) => new { AlbumId = artistAssign.AlbumId, tagAssign.TagId, TagName = tagAssign.Tag.Name });
                countsQuery = countsQuery.Concat(artistCounts);
            }

            var counts = await countsQuery
                .Distinct()
                .GroupBy(ass => new { ass.TagId, ass.TagName })
                .Select(g => new { id = g.Key.TagId, name = g.Key.TagName, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .ToListAsync();

            return counts.Cast<object>().ToList();
        }

        public async Task<object> GetUserAlbumTagsAsync()
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var tagsList = await context.LibTags
                .Where(t => t.TagGroup.UserId == userId && t.ForAlbum)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.TagGroupId })
                .ToListAsync();

            return tagsList;
        }

        public async Task<List<object>> GetAlbumsArtistsForUserAsync(
            string? tags = null,
            int? fromYear = null,
            int? toYear = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? includeArtists = null,
            string? excludeArtists = null,
            string? includeGenres = null,
            string? excludeGenres = null,
            bool includeArtistTags = false,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Album)
                .AsQueryable();

            if (fromYear.HasValue)
                query = query.Where(album => album.Year >= fromYear.Value);

            if (toYear.HasValue)
                query = query.Where(album => album.Year <= toYear.Value);

            var includedArtistIds = ParseSpotifyIds(includeArtists);
            if (includedArtistIds.Count > 0)
                query = query.Where(album => album.AlbumArtistAssigns.Any(assign => includedArtistIds.Contains(assign.ArtistId)));

            var excludedArtistIds = ParseSpotifyIds(excludeArtists);
            if (excludedArtistIds.Count > 0)
                query = query.Where(album => !album.AlbumArtistAssigns.Any(assign => excludedArtistIds.Contains(assign.ArtistId)));

            query = ApplyAlbumTags(query, userId, tags, "or", null, null, includeArtistTags);

            query = ApplyAlbumTags(query, userId, includeTags, includeMode, excludeTags, excludeMode, includeArtistTags);
            query = ApplyAlbumGenres(query, includeGenres, excludeGenres);

            foreach (var term in GetSearchTerms(search))
            {
                var termLocal = term;
                query = query.Where(album =>
                    (album.Name != null && EF.Functions.Like(album.Name.ToLower(), "%" + termLocal + "%"))
                    || EF.Functions.Like(album.Id.ToLower(), "%" + termLocal + "%")
                    || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "album" && note.SpotifyItemId == album.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%"))
                    || context.SpAlbumArtistAssigns.Any(assign =>
                        assign.AlbumId == album.Id
                        && ((assign.Artist.Name != null && EF.Functions.Like(assign.Artist.Name.ToLower(), "%" + termLocal + "%"))
                            || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == assign.ArtistId && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%")))));
            }

            var artistInfos = await query
                .SelectMany(album => album.AlbumArtistAssigns)
                .Select(assign => new { Id = assign.ArtistId, Name = assign.ArtistName })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return artistInfos.Cast<object>().ToList();
        }

        public async Task<List<string>> GetAlbumGenresForUserAsync(
            string? tags = null,
            string? artistId = null,
            string? includeArtists = null,
            string? excludeArtists = null,
            int? fromYear = null,
            int? toYear = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            bool includeArtistTags = false,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibAlbumUserAssigns
                .Where(assign => assign.UserId == userId)
                .Select(assign => assign.Album)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var artistIdLocal = artistId.Trim();
                query = query.Where(album => album.AlbumArtistAssigns.Any(assign => assign.ArtistId == artistIdLocal));
            }

            var includedArtistIds = ParseSpotifyIds(includeArtists);
            if (includedArtistIds.Count > 0)
                query = query.Where(album => album.AlbumArtistAssigns.Any(assign => includedArtistIds.Contains(assign.ArtistId)));

            var excludedArtistIds = ParseSpotifyIds(excludeArtists);
            if (excludedArtistIds.Count > 0)
                query = query.Where(album => !album.AlbumArtistAssigns.Any(assign => excludedArtistIds.Contains(assign.ArtistId)));

            if (fromYear.HasValue)
                query = query.Where(album => album.Year >= fromYear.Value);
            if (toYear.HasValue)
                query = query.Where(album => album.Year <= toYear.Value);

            query = ApplyAlbumTags(query, userId, tags, "or", null, null, includeArtistTags);

            query = ApplyAlbumTags(query, userId, includeTags, includeMode, excludeTags, excludeMode, includeArtistTags);

            foreach (var term in GetSearchTerms(search))
            {
                var termLocal = term;
                query = query.Where(album =>
                    (album.Name != null && EF.Functions.Like(album.Name.ToLower(), "%" + termLocal + "%"))
                    || EF.Functions.Like(album.Id.ToLower(), "%" + termLocal + "%")
                    || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "album" && note.SpotifyItemId == album.Id && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%"))
                    || album.AlbumArtistAssigns.Any(assign =>
                        (assign.Artist.Name != null && EF.Functions.Like(assign.Artist.Name.ToLower(), "%" + termLocal + "%"))
                        || context.LibItemNotes.Any(note => note.UserId == userId && note.ItemType == "artist" && note.SpotifyItemId == assign.ArtistId && EF.Functions.Like(note.Content.ToLower(), "%" + termLocal + "%"))));
            }

            var rawGenres = await query
                .SelectMany(album => album.AlbumArtistAssigns)
                .Select(assign => assign.Artist.Genres)
                .Where(genres => genres != null && genres != string.Empty)
                .Distinct()
                .ToListAsync();

            return rawGenres
                .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(genre => genre.Trim())
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(genre => genre, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"), ignoreCase: true))
                .ToList();
        }

        private static List<string> ParseSpotifyIds(string? value) => string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToList();

        public async Task<object> GetUserPlaylistsAsync(
            string? sort = null,
            string? order = null,
            string? search = null,
            long? folderId = null,
            int page = 1,
            int pageSize = 50)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.SpPlaylists
                .AsQueryable()
                .Where(p => p.OwnerId == userId);

            if (folderId.HasValue)
                query = query.Where(p => p.FolderId == folderId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLower()).ToList();

                foreach (var term in terms)
                {
                    var tlocal = term;
                    query = query.Where(p => p.Name != null && EF.Functions.Like(p.Name.ToLower(), "%" + tlocal + "%"));
                }
            }

            var sortKey = (sort ?? "name").ToLowerInvariant();
            var desc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            query = sortKey switch
            {
                "tracks" => desc ? query.OrderByDescending(p => p.TotalTracks).ThenBy(p => p.Name) : query.OrderBy(p => p.TotalTracks).ThenBy(p => p.Name),
                _ => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            };

            var total = await query.CountAsync();

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            var pageItems = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Dołącz przypisany folder z tabeli assign
            var pageIds = pageItems.Select(x => x.Id).ToList();
            Dictionary<string, long?> assigns;
            try
            {
                assigns = await context.LibPlaylistFolderAssigns
                    .Where(a => a.UserId == userId && pageIds.Contains(a.PlaylistId))
                    .ToDictionaryAsync(a => a.PlaylistId, a => a.FolderId);
            }
            catch
            {
                // Jeśli tabela nie istnieje (migracja nie zastosowana), zwróć bez przypisań
                assigns = new Dictionary<string, long?>();
            }

            var items = pageItems.Select(p => p.MapToPlaylistModel(assigns.GetValueOrDefault(p.Id))).ToList();

            return new { total, page, pageSize, items };
        }

        public async Task<object> GetPlaylistTracksAsync(string playlistId, int page = 1, int pageSize = 100)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            // Weryfikuj dostęp do playlisty: musi należeć do użytkownika
            var playlist = await context.SpPlaylists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == playlistId && p.OwnerId == userId);
            if (playlist == null)
                throw new InvalidOperationException("Playlista nie należy do użytkownika lub nie istnieje");

            var q = context.SpPlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .Include(pt => pt.Track)!
                    .ThenInclude(t => t.Album)
                .Include(pt => pt.Track)!
                    .ThenInclude(t => t.TrackArtists)!
                        .ThenInclude(ta => ta.Artist)
                .Select(pt => pt.Track!)
                .AsQueryable();

            var total = await q.CountAsync();

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 100;

            var tracks = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var trackIds = tracks.Select(t => t.Id).ToList();
            var trackTags = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "track" && trackIds.Contains(ass.SpotifyItemId))
                .Include(ass => ass.Tag)
                .ThenInclude(t => t.TagGroup)
                .ToListAsync();

            var tagsByTrack = trackTags
                .GroupBy(ass => ass.SpotifyItemId)
                .ToDictionary(g => g.Key, g => g.Select(ass => ass.MapToTrackTagModel(ass.Tag, ass.Tag.TagGroup)).ToList());

            var items = tracks.Select(t => t.MapToTrackModel(tagsByTrack.GetValueOrDefault(t.Id, new List<TrackTagModel>()))).ToList();

            return new { total, page, pageSize, items };
        }

        public async Task<List<object>> GetPlaylistTrackTagCountsAsync(
            string playlistId,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null,
            string? search = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var playlist = await context.SpPlaylists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == playlistId && p.OwnerId == userId);
            if (playlist == null)
                throw new InvalidOperationException("Playlista nie należy do użytkownika lub nie istnieje");

            var q = context.SpPlaylistTracks
                .Where(pt => pt.PlaylistId == playlistId)
                .Include(pt => pt.Track)!
                    .ThenInclude(t => t.TrackArtists)!
                        .ThenInclude(ta => ta.Artist)
                .Select(pt => pt.Track!)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(includeTags))
            {
                var tagIds = includeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(includeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                    {
                        foreach (var tid in tagIds)
                        {
                            var idLocal = tid;
                            q = q.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        q = q.Where(track => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(excludeTags))
            {
                var tagIds = excludeTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
                if (tagIds.Count > 0)
                {
                    var andMode = string.Equals(excludeMode, "and", StringComparison.OrdinalIgnoreCase);
                    if (andMode)
                        q = q.Where(track => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && ass.TagId == tid)));
                    else
                        q = q.Where(track => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == track.Id && tagIds.Contains(ass.TagId)));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(term => term.ToLower())
                    .ToList();

                foreach (var term in terms)
                {
                    var termLocal = term;
                    q = q.Where(track =>
                        (track.Name != null && EF.Functions.Like(track.Name.ToLower(), "%" + termLocal + "%"))
                        || (track.Album != null && track.Album.Name != null && EF.Functions.Like(track.Album.Name.ToLower(), "%" + termLocal + "%"))
                        || context.SpTrackArtists.Any(assign => assign.TrackId == track.Id && assign.Artist.Name != null && EF.Functions.Like(assign.Artist.Name.ToLower(), "%" + termLocal + "%"))
                        || context.LibTagAssigns.Any(assign => assign.UserId == userId && assign.ItemType == "track" && assign.SpotifyItemId == track.Id && context.LibTags.Any(tag => tag.Id == assign.TagId && EF.Functions.Like(tag.Name.ToLower(), "%" + termLocal + "%"))));
                }
            }

            var trackIds = await q.Select(t => t.Id).ToListAsync();

            var counts = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "track" && trackIds.Contains(ass.SpotifyItemId))
                .GroupBy(ass => new { ass.TagId, ass.Tag.Name })
                .Select(g => new { id = g.Key.TagId, name = g.Key.Name, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .ToListAsync();

            return counts.Cast<object>().ToList();
        }

        public async Task<object?> GetSingleTrackAsync(string trackId)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var t = await context.SpTracks
                .Include(x => x.Album)
                .Include(x => x.TrackArtists)!.ThenInclude(ta => ta.Artist)
                .FirstOrDefaultAsync(x => x.Id == trackId);

            if (t == null)
                return null;

            var tagAssigns = await context.LibTagAssigns
                .Where(a => a.UserId == userId && a.ItemType == "track" && a.SpotifyItemId == trackId)
                .Include(a => a.Tag).ThenInclude(g => g.TagGroup)
                .ToListAsync();

            var tags = tagAssigns.Select(a => a.MapToTrackTagModel(a.Tag, a.Tag.TagGroup)).ToList();

            return t.MapToTrackModel(tags);
        }

        public async Task AssignPlaylistToFolderAsync(string playlistId, long? folderId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var playlist = await context.SpPlaylists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == playlistId && p.OwnerId == userId);
            if (playlist == null)
                throw new InvalidOperationException("Playlista nie należy do użytkownika lub nie istnieje");

            if (folderId.HasValue)
            {
                var folderExists = await context.LibPlaylistFolders.AnyAsync(f => f.Id == folderId.Value && f.UserId == userId);
                if (!folderExists)
                    throw new InvalidOperationException("Folder nie istnieje");
            }

            var entity = await context.LibPlaylistFolderAssigns.FirstOrDefaultAsync(a => a.UserId == userId && a.PlaylistId == playlistId);
            if (entity == null)
            {
                entity = new LibPlaylistFolderAssign
                {
                    UserId = userId,
                    PlaylistId = playlistId,
                    FolderId = folderId,
                    DateCreatedUtc = DateTime.UtcNow
                };
                context.LibPlaylistFolderAssigns.Add(entity);
            }
            else
            {
                entity.FolderId = folderId;
            }

            await context.SaveChangesAsync();
        }

        public async Task UpdateFolderParentAsync(long folderId, long? parentId)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var folder = await context.LibPlaylistFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);
            if (folder == null)
                throw new InvalidOperationException("Folder nie istnieje");

            if (parentId.HasValue)
            {
                if (parentId.Value == folderId)
                    throw new InvalidOperationException("Folder nie może być swoim rodzicem");

                var parent = await context.LibPlaylistFolders.FirstOrDefaultAsync(f => f.Id == parentId.Value && f.UserId == userId);
                if (parent == null)
                    throw new InvalidOperationException("Folder nadrzędny nie istnieje");
            }

            folder.ParentId = parentId;
            await context.SaveChangesAsync();
        }
    }
}
