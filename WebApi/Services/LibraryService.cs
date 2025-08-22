using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;

namespace SpotifyBasicApi2025.Services
{
    public class LibraryService
    {
        private readonly DatabaseContext context;
        private readonly UserService userService;
        private readonly SpotifyRequestService spotifyRequestService;
        private readonly SpotifyApiService spotifyApiService;

        public LibraryService(DatabaseContext context, UserService userService, SpotifyRequestService spotifyRequestService, SpotifyApiService spotifyApiService)
        {
            this.context = context;
            this.userService = userService;
            this.spotifyRequestService = spotifyRequestService;
            this.spotifyApiService = spotifyApiService;
        }

        public async Task<object> GetUserArtistsAsync(
            string? sort = null,
            string? order = null,
            string? tags = null,
            long? sortTagId = null,
            string? genre = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibArtistUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Artist)
                .AsQueryable();

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

            // Sortowanie
            var sortKey = (sort ?? "name").ToLowerInvariant();
            var sortOrderDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            if (sortKey == "weight" && sortTagId.HasValue)
            {
                // Sortuj po sile wybranego tagu (brak = 0)
                query = sortOrderDesc
                    ? query.OrderByDescending(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == a.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault())
                    : query.OrderBy(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "artist" && ass.SpotifyItemId == a.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault());
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

            // Mapuj artystów do modeli z tagami
            var artistModels = artists.Select(artist =>
            {
                var tags = tagsByArtist.GetValueOrDefault(artist.Id, new List<ArtistTagModel>());
                return artist.MapToArtistModel(tags);
            }).ToList();

            // Dodatkowo dołącz bieżącą siłę wybranego tagu (jeśli podano)
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
            string? excludeTags = null,
            string? excludeMode = null,
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

            if (sortKey == "weight" && sortTagId.HasValue)
            {
                query = sortDesc
                    ? query.OrderByDescending(t => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault())
                    : query.OrderBy(t => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "track" && ass.SpotifyItemId == t.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault());
            }
            else
            {
                query = sortKey switch
                {
                    "year" => sortDesc ? query.OrderByDescending(x => x.Album!.ReleaseDate) : query.OrderBy(x => x.Album!.ReleaseDate),
                    "popularity" => sortDesc ? query.OrderByDescending(x => x.Popularity) : query.OrderBy(x => x.Popularity),
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
        public async Task<object> GetUserAlbumsAsync(
            string? sort = null,
            string? order = null,
            string? tags = null,
            long? sortTagId = null,
            string? artistId = null,
            string? includeTags = null,
            string? includeMode = null,
            string? excludeTags = null,
            string? excludeMode = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .Include(a => a.Album)
                    .ThenInclude(al => al.AlbumArtistAssigns)
                .Select(a => a.Album)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                var aid = artistId.Trim();
                query = query.Where(album => context.SpAlbumArtistAssigns.Any(x => x.AlbumId == album.Id && x.ArtistId == aid));
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagIds = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.Parse(id))
                    .ToList();

                if (tagIds.Count > 0)
                    query = query.Where(album => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == album.Id && tagIds.Contains(ass.TagId)));
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
                            query = query.Where(album => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == album.Id && ass.TagId == idLocal));
                        }
                    }
                    else
                    {
                        query = query.Where(album => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == album.Id && tagIds.Contains(ass.TagId)));
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
                        query = query.Where(album => !tagIds.All(tid => context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == album.Id && ass.TagId == tid)));
                    }
                    else
                    {
                        query = query.Where(album => !context.LibTagAssigns.Any(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == album.Id && tagIds.Contains(ass.TagId)));
                    }
                }
            }

            var sortKey = (sort ?? "name").ToLowerInvariant();
            var sortOrderDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

            if (sortKey == "weight" && sortTagId.HasValue)
            {
                query = sortOrderDesc
                    ? query.OrderByDescending(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == a.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault())
                    : query.OrderBy(a => context.LibTagAssigns.Where(ass => ass.UserId == userId && ass.ItemType == "album" && ass.SpotifyItemId == a.Id && ass.TagId == sortTagId.Value).Select(ass => ass.Strength).FirstOrDefault());
            }
            else
            {
                query = sortKey switch
                {
                    "year" => sortOrderDesc ? query.OrderByDescending(x => x.ReleaseDate) : query.OrderBy(x => x.ReleaseDate),
                    "popularity" => sortOrderDesc ? query.OrderByDescending(x => x.Popularity) : query.OrderBy(x => x.Popularity),
                    _ => sortOrderDesc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                };
            }

            var albums = new List<SpAlbum>();

            try
            {
                albums = await query.ToListAsync();
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                throw ex;
            }

            var albumIds = albums.Select(a => a.Id).ToList();
            var albumTags = await context.LibTagAssigns
                .Where(ass => ass.UserId == userId && ass.ItemType == "album" && albumIds.Contains(ass.SpotifyItemId))
                .Include(ass => ass.Tag)
                .ThenInclude(t => t.TagGroup)
                .ToListAsync();

            var tagsByAlbum = albumTags
                .GroupBy(ass => ass.SpotifyItemId)
                .ToDictionary(g => g.Key, g => g.Select(ass => ass.MapToAlbumTagModel(ass.Tag, ass.Tag.TagGroup)).ToList());

            var albumModels = albums.Select(album =>
            {
                var tagsList = tagsByAlbum.GetValueOrDefault(album.Id, new List<AlbumTagModel>());
                return album.MapToAlbumModel(tagsList);
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
                        TagStrength = a.Tags.FirstOrDefault(t => t.TagId == sortTagId.Value)?.Strength ?? 0f,
                        Tags = a.Tags
                    })
                    .ToList();

                return new { total = albumDtos.Count, items = albumDtos };
            }

            return new { total = albumModels.Count, items = albumModels };
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

        public async Task<List<object>> GetAlbumsArtistsForUserAsync()
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var artistInfos = await context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .SelectMany(a => a.Album.AlbumArtistAssigns)
                .Select(x => new { Id = x.ArtistId, Name = x.ArtistName })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return artistInfos.Cast<object>().ToList();
        }
    }
}