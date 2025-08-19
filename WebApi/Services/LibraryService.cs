using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;

namespace SpotifyBasicApi2025.Services
{
    public class LibraryService
    {
        private readonly DatabaseContext context;
        private readonly UserService userService;

        public LibraryService(DatabaseContext context, UserService userService)
        {
            this.context = context;
            this.userService = userService;
        }

        public async Task<object> GetUserArtistsAsync(string? sort = null, string? order = null, string? tags = null, long? sortTagId = null, string? genre = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Użytkownik nie jest zalogowany");

            var query = context.LibArtistUserAssigns
                .Where(a => a.UserId == userId)
                .Select(a => a.Artist)
                .AsQueryable();

            // Filtrowanie po tagach (ANY)
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
    }
}