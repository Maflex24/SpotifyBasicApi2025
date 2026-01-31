using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyApiService
    {
        private readonly SpotifyRequestService requestService;
        private readonly DatabaseContext context;

        public SpotifyApiService(SpotifyRequestService requestService, DatabaseContext context)
        {
            this.requestService = requestService;
            this.context = context;
        }

        #region Artists

        public async Task<SpotifyArtistObject?> GetArtist(string id)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"artists/{id}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyArtistObject>(response.ToString()!);
        }

        public async Task<SpotifyArtistObject?> GetAndSaveArtist(string id)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"artists/{id}");

            if (response == null)
                return null;

            var model = JsonSerializer.Deserialize<SpotifyArtistObject>(response.ToString()!);

            await SaveOrUpdateArtistAsync(model);

            return model;
        }

        public async Task<SpArtist> SaveOrUpdateArtistAsync(SpotifyArtistObject model)
        {
            var artist = context.SpArtists.FirstOrDefault(a => a.Id == model.Id);

            if (artist is null)
            {
                artist = model.MapToEntity(true);
                context.SpArtists.Add(artist);
            }
            else
            {
                artist = model.MapToEntity();
            }

            await context.SaveChangesAsync();

            return artist;
        }

        public async Task<List<SpotifyTrackObject>> GetArtistTopTracksAsync(string artistId, string market = "PL")
        {
            var response = await requestService.MakeSpotifyRequestAsync($"artists/{artistId}/top-tracks?market={market}");

            if (response == null)
                return new List<SpotifyTrackObject>();

            try
            {
                var json = response.ToString() ?? string.Empty;
                var parsed = JsonSerializer.Deserialize<TopTracksResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed?.Tracks?.Count > 0)
                    return parsed.Tracks;

                return new List<SpotifyTrackObject>();
            }
            catch
            {
                return new List<SpotifyTrackObject>();
            }
        }

        public async Task<List<SpotifyAlbumObject>> GetArtistAlbumsAsync(string artistId, string includeGroups = "album,single", string market = "PL")
        {
            var allAlbums = new List<SpotifyAlbumObject>();
            var limit = 50;
            var offset = 0;

            while (true)
            {
                var url = $"artists/{artistId}/albums?include_groups={includeGroups}&market={market}&limit={limit}&offset={offset}";
                var response = await requestService.MakeSpotifyRequestAsync(url);
                if (response == null)
                    break;

                try
                {
                    var json = response.ToString() ?? string.Empty;
                    var parsed = JsonSerializer.Deserialize<SpotifyArtistAlbumsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed?.Items == null || parsed.Items.Count == 0)
                        break;

                    allAlbums.AddRange(parsed.Items);

                    if (string.IsNullOrEmpty(parsed.Next))
                        break;

                    offset += limit;
                }
                catch
                {
                    break;
                }
            }

            // deduplicate by id (Spotify often returns duplicates between groups)
            var dedup = allAlbums
                .GroupBy(a => a.Id)
                .Select(g => g.First())
                .OrderByDescending(a => a.ReleaseDate)
                .ToList();

            return dedup;
        }

        private sealed class SpotifyArtistAlbumsResponse
        {
            public List<SpotifyAlbumObject> Items { get; set; } = new();
            public string? Next { get; set; }
        }

        private sealed class TopTracksResponse
        {
            [JsonPropertyName("tracks")] public List<SpotifyTrackObject> Tracks { get; set; } = new();
        }

        public async Task<SpotifyFollowedArtistsResponse?> GetFollowedArtists(int limit = 50)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"me/following?type=artist&limit={limit}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyFollowedArtistsResponse>(response.ToString()!);
        }

        public async Task<List<SpotifyArtistObject>> GetAllFollowedArtists()
        {
            var allArtists = new List<SpotifyArtistObject>();
            string? after = null;
            const int limit = 50; // Maksymalny limit zgodnie z dokumentacją

            do
            {
                var endpoint = $"me/following?type=artist&limit={limit}";

                if (!string.IsNullOrEmpty(after))
                {
                    endpoint += $"&after={after}";
                }

                var response = await requestService.MakeSpotifyRequestAsync(endpoint);

                if (response == null)
                    break;

                var followedArtistsResponse = JsonSerializer.Deserialize<SpotifyFollowedArtistsResponse>(response.ToString()!);

                if (followedArtistsResponse?.Artists?.Items == null)
                    break;

                allArtists.AddRange(followedArtistsResponse.Artists.Items);

                // Pobierz cursor "after" dla następnej strony
                after = followedArtistsResponse.Artists.Cursors?.After;

                // Jeśli nie ma więcej stron, zakończ
                if (string.IsNullOrEmpty(after))
                    break;
            } while (true);

            return allArtists;
        }

        public async Task<object> SyncUserArtists()
        {
            var spotifyArtists = await GetAllFollowedArtists();

            var savedCount = await SaveFollowedArtistsAsync(spotifyArtists);
            var savedAssign = await SaveArtistUserAssign(spotifyArtists);

            return new
            {
                message = $"Zapisano {savedCount} artystów w bazie danych",
                totalArtists = spotifyArtists.Count,
                savedCount = savedCount,
                savedAssign = savedAssign
            };
        }

        #endregion Artists

        #region Playlists

        public async Task<object?> GetUserPlaylistsAsync()
        {
            return await requestService.MakeSpotifyRequestAsync("me/playlists");
        }

        #endregion Playlists

        #region Profile

        public async Task<SpotifyUserProfile?> GetCurrentUserProfileAsync()
        {
            var response = await requestService.MakeSpotifyRequestAsync("me");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyUserProfile>(response.ToString()!);
        }

        #endregion Profile

        #region Search

        public async Task<object?> SearchTracksAsync(string query, int limit = 20)
        {
            var encodedQuery = Uri.EscapeDataString(query);

            return await requestService.MakeSpotifyRequestAsync($"search?q={encodedQuery}&type=track&limit={limit}");
        }

        #endregion Search

        #region Database Operations

        // Zapisywanie użytkownika z profilu Spotify
        public async Task<SpUser> SaveUserFromSpotifyProfileAsync(SpotifyUserProfile spotifyProfile)
        {
            // Sprawdź czy użytkownik już istnieje
            var existingUser = await context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == spotifyProfile.Id);

            if (existingUser != null)
            {
                // Aktualizuj istniejącego użytkownika
                var updatedUser = spotifyProfile.MapToEntity();
                updatedUser.CreatedAt = existingUser.CreatedAt; // Zachowaj oryginalną datę utworzenia

                context.Entry(existingUser).CurrentValues.SetValues(updatedUser);
                await context.SaveChangesAsync();
                return existingUser;
            }
            else
            {
                // Dodaj nowego użytkownika
                var newUser = spotifyProfile.MapToEntity(newEntity: true);
                context.SpUsers.Add(newUser);
                await context.SaveChangesAsync();

                return newUser;
            }
        }

        // Zapisywanie śledzonych artystów z Spotify API
        public async Task<int> SaveFollowedArtistsAsync(List<SpotifyArtistObject> spotifyArtists)
        {
            var savedCount = 0;

            foreach (var spotifyArtist in spotifyArtists)
            {
                // Sprawdź czy artysta już istnieje
                var existingArtist = await context.SpArtists
                    .FirstOrDefaultAsync(a => a.Id == spotifyArtist.Id);

                if (existingArtist != null)
                {
                    // Aktualizuj istniejącego artystę
                    var updatedArtist = spotifyArtist.MapToEntity();
                    updatedArtist.CreatedAt = existingArtist.CreatedAt; // Zachowaj oryginalną datę utworzenia

                    context.Entry(existingArtist).CurrentValues.SetValues(updatedArtist);
                }
                else
                {
                    // Dodaj nowego artystę
                    var newArtist = spotifyArtist.MapToEntity(newEntity: true);
                    context.SpArtists.Add(newArtist);
                }

                savedCount++;
            }

            await context.SaveChangesAsync();
            return savedCount;
        }

        public async Task<int> SaveArtistUserAssign(List<SpotifyArtistObject> spotifyArtists)
        {
            // Pobierz userId z profilu użytkownika
            var userProfile = await GetCurrentUserProfileAsync();
            var userId = userProfile?.Id;

            if (string.IsNullOrEmpty(userId))
                return 0;

            var existedAssigns = await context.LibArtistUserAssigns
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var savedCount = 0;

            foreach (var spotifyArtist in spotifyArtists)
            {
                // Sprawdź czy przypis już istnieje
                var existingAssign = existedAssigns
                    .FirstOrDefault(a =>
                        a.ArtistId == spotifyArtist.Id &&
                        a.UserId == userId
                    );

                if (existingAssign != null)
                {
                    // Aktualizuj
                    var updatedAssign = spotifyArtist.MapToArtistUserAssignEntity(userId);
                    updatedAssign.Id = existingAssign.Id; // Zachowaj oryginalne ID
                    updatedAssign.DateCreatedUtc = existingAssign.DateCreatedUtc; // Zachowaj oryginalną datę utworzenia

                    context.Entry(existingAssign).CurrentValues.SetValues(updatedAssign);
                }
                else
                {
                    // Dodaj
                    var newAssign = spotifyArtist.MapToArtistUserAssignEntity(userId);
                    context.LibArtistUserAssigns.Add(newAssign);
                }

                savedCount++;
            }

            // Sprawdzamy, czy nie ma jakiś przypisań, które powinny zostać usunięte
            foreach (var assign in existedAssigns)
            {
                var artistStillInLibrary = spotifyArtists.Exists(a => a.Id == assign.ArtistId);

                if (!artistStillInLibrary)
                    context.LibArtistUserAssigns.Remove(assign);
            }

            await context.SaveChangesAsync();
            return savedCount;
        }

        // Pobieranie wszystkich artystów z bazy danych
        public async Task<List<SpArtist>> GetAllArtistsAsync()
        {
            return await context.SpArtists
                .OrderByDescending(a => a.Popularity)
                .ToListAsync();
        }

        // Pobieranie artystów po gatunku
        public async Task<List<SpArtist>> GetArtistsByGenreAsync(string genre)
        {
            return await context.SpArtists
                .Where(a => a.Genres != null && a.Genres.Contains(genre))
                .OrderByDescending(a => a.Popularity)
                .ToListAsync();
        }

        // Pobieranie artystów o najwyższej popularności
        public async Task<List<SpArtist>> GetTopArtistsAsync(int limit = 10)
        {
            return await context.SpArtists
                .OrderByDescending(a => a.Popularity)
                .Take(limit)
                .ToListAsync();
        }

        // Pobieranie użytkownika po ID
        public async Task<SpUser?> GetUserByIdAsync(string userId)
        {
            return await context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // Pobieranie wszystkich użytkowników
        public async Task<List<SpUser>> GetAllUsersAsync()
        {
            return await context.SpUsers
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        // Zapisywanie albumu z modelu Spotify
        public async Task<SpAlbum> SaveAlbumFromSpotifyAsync(SpotifyAlbumObject spotifyAlbum)
        {
            // Sprawdź czy album już istnieje
            var existingAlbum = await context.SpAlbums
                .FirstOrDefaultAsync(a => a.Id == spotifyAlbum.Id);

            if (existingAlbum != null)
            {
                // Aktualizuj istniejący album
                var updatedAlbum = spotifyAlbum.MapToEntity();
                updatedAlbum.CreatedAt = existingAlbum.CreatedAt; // Zachowaj oryginalną datę utworzenia

                context.Entry(existingAlbum).CurrentValues.SetValues(updatedAlbum);
                await context.SaveChangesAsync();
                return existingAlbum;
            }
            else
            {
                // Dodaj nowy album
                var newAlbum = spotifyAlbum.MapToEntity(newEntity: true);
                context.SpAlbums.Add(newAlbum);
                await context.SaveChangesAsync();

                return newAlbum;
            }
        }

        // Zapisywanie utworu z modelu Spotify
        public async Task<SpTrack> SaveTrackFromSpotifyAsync(SpotifyTrackObject spotifyTrack)
        {
            // Sprawdź czy utwór już istnieje
            var existingTrack = await context.SpTracks
                .FirstOrDefaultAsync(t => t.Id == spotifyTrack.Id);

            if (existingTrack != null)
            {
                // Aktualizuj istniejący utwór
                var updatedTrack = spotifyTrack.MapToEntity();
                updatedTrack.CreatedAt = existingTrack.CreatedAt; // Zachowaj oryginalną datę utworzenia

                context.Entry(existingTrack).CurrentValues.SetValues(updatedTrack);
                await context.SaveChangesAsync();
                return existingTrack;
            }
            else
            {
                // Dodaj nowy utwór
                var newTrack = spotifyTrack.MapToEntity(newEntity: true);
                context.SpTracks.Add(newTrack);
                await context.SaveChangesAsync();

                return newTrack;
            }
        }

        public async Task<SpotifyTrackObject?> GetTrackAsync(string id)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"tracks/{id}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyTrackObject>(response.ToString()!);
        }

        // Zapisywanie zapisanych albumów użytkownika
        public async Task<int> SaveUserSavedAlbumsAsync(List<SpotifySavedAlbumItem> savedAlbums, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 0;

            var existingSavedAlbums = await context.SpSavedAlbums
                .Where(sa => sa.UserId == userId)
                .ToListAsync();

            var savedCount = 0;

            foreach (var savedAlbum in savedAlbums)
            {
                // Sprawdź czy zapisany album już istnieje
                var existingSavedAlbum = existingSavedAlbums
                    .FirstOrDefault(sa => sa.AlbumId == savedAlbum.Album.Id && sa.UserId == userId);

                if (existingSavedAlbum != null)
                {
                    // Aktualizuj istniejący zapisany album
                    var updatedSavedAlbum = savedAlbum.MapToEntity(userId);
                    updatedSavedAlbum.Id = existingSavedAlbum.Id; // Zachowaj oryginalne ID
                    updatedSavedAlbum.CreatedAt = existingSavedAlbum.CreatedAt; // Zachowaj oryginalną datę utworzenia

                    context.Entry(existingSavedAlbum).CurrentValues.SetValues(updatedSavedAlbum);
                }
                else
                {
                    // Dodaj nowy zapisany album
                    var newSavedAlbum = savedAlbum.MapToEntity(userId);
                    context.SpSavedAlbums.Add(newSavedAlbum);
                }

                savedCount++;
            }

            // Usuń zapisane albumy, które nie są już w bibliotece użytkownika
            foreach (var existingSavedAlbum in existingSavedAlbums)
            {
                var albumStillInLibrary = savedAlbums.Exists(sa => sa.Album.Id == existingSavedAlbum.AlbumId);

                if (!albumStillInLibrary)
                    context.SpSavedAlbums.Remove(existingSavedAlbum);
            }

            await context.SaveChangesAsync();
            return savedCount;
        }

        // Zapisywanie przypisań albumów do użytkownika
        public async Task<int> SaveAlbumUserAssign(List<SpotifyAlbumObject> spotifyAlbums)
        {
            // Pobierz userId z profilu użytkownika
            var userProfile = await GetCurrentUserProfileAsync();
            var userId = userProfile?.Id;

            if (string.IsNullOrEmpty(userId))
                return 0;

            var existedAssigns = await context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var savedCount = 0;

            foreach (var spotifyAlbum in spotifyAlbums)
            {
                // Sprawdź czy przypis już istnieje
                var existingAssign = existedAssigns
                    .FirstOrDefault(a =>
                        a.AlbumId == spotifyAlbum.Id &&
                        a.UserId == userId
                    );

                if (existingAssign != null)
                {
                    // Aktualizuj
                    var updatedAssign = spotifyAlbum.MapToAlbumUserAssignEntity(userId);
                    updatedAssign.Id = existingAssign.Id; // Zachowaj oryginalne ID
                    updatedAssign.DateCreatedUtc = existingAssign.DateCreatedUtc; // Zachowaj oryginalną datę utworzenia

                    context.Entry(existingAssign).CurrentValues.SetValues(updatedAssign);
                }
                else
                {
                    // Dodaj
                    var newAssign = spotifyAlbum.MapToAlbumUserAssignEntity(userId);
                    context.LibAlbumUserAssigns.Add(newAssign);
                }

                savedCount++;
            }

            // Sprawdzamy, czy nie ma jakiś przypisań, które powinny zostać usunięte
            foreach (var assign in existedAssigns)
            {
                var albumStillInLibrary = spotifyAlbums.Exists(a => a.Id == assign.AlbumId);

                if (!albumStillInLibrary)
                    context.LibAlbumUserAssigns.Remove(assign);
            }

            await context.SaveChangesAsync();
            return savedCount;
        }

        #endregion Database Operations

        #region Albums

        public async Task<SpotifySavedAlbumsResponse?> GetUserSavedAlbums(int limit = 50)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"me/albums?limit={limit}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifySavedAlbumsResponse>(response.ToString()!);
        }

        public async Task<SpotifyAlbumObject?> GetAlbumAsync(string albumId)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"albums/{albumId}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyAlbumObject>(response.ToString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<List<SpotifyTrackObject>> GetAllAlbumTracks(string albumId)
        {
            var all = new List<SpotifyTrackObject>();
            var limit = 50;
            var offset = 0;

            while (true)
            {
                var response = await requestService.MakeSpotifyRequestAsync($"albums/{albumId}/tracks?limit={limit}&offset={offset}");

                if (response == null)
                    break;

                try
                {
                    var json = response.ToString() ?? string.Empty;
                    var parsed = JsonSerializer.Deserialize<AlbumTracksResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed?.Items == null || parsed.Items.Count == 0)
                        break;
                    all.AddRange(parsed.Items);
                    if (string.IsNullOrEmpty(parsed.Next))
                        break;
                    offset += limit;
                }
                catch { break; }
            }

            return all;
        }

        private sealed class AlbumTracksResponse
        { public List<SpotifyTrackObject> Items { get; set; } = new(); public string? Next { get; set; } }

        public async Task<List<SpotifySavedAlbumItem>> GetAllUserSavedAlbums()
        {
            var allSavedAlbums = new List<SpotifySavedAlbumItem>();
            string? next = null;
            const int limit = 50;

            do
            {
                var endpoint = $"me/albums?limit={limit}";

                if (!string.IsNullOrEmpty(next))
                {
                    endpoint = next;
                }

                var response = await requestService.MakeSpotifyRequestAsync(endpoint);

                if (response == null)
                    break;

                var savedAlbumsResponse = JsonSerializer.Deserialize<SpotifySavedAlbumsResponse>(response.ToString()!);

                if (savedAlbumsResponse?.Items == null)
                    break;

                allSavedAlbums.AddRange(savedAlbumsResponse.Items);
                next = savedAlbumsResponse.Next;
            } while (!string.IsNullOrEmpty(next));

            return allSavedAlbums;
        }

        public async Task<object> SyncUserSavedAlbums()
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
                throw new InvalidOperationException("Nie można pobrać profilu użytkownika");

            var userId = userProfile.Id;
            var allSavedAlbums = await GetAllUserSavedAlbumsParallel(pageSize: 50, maxDegreeOfParallelism: 6);

            if (allSavedAlbums == null || !allSavedAlbums.Any())
                return new { message = "Brak zapisanych albumów do synchronizacji", savedCount = 0 };

            var savedCount = 0;
            var artistsCount = 0;
            var albumsCount = 0;
            var tracksCount = 0;

            // Kolekcje do zbierania wszystkich encji przed zapisaniem
            var allArtists = new Dictionary<string, SpotifyArtistObject>();
            var allAlbums = new Dictionary<string, SpotifyAlbumObject>();
            var allTracks = new Dictionary<string, SpotifyTrackObject>();
            var allTrackArtists = new List<(string TrackId, SpotifyArtistObject Artist)>();
            var allAlbumArtists = new List<(string AlbumId, SpotifyArtistObject Artist)>();

            // Krok 1: Zbierz wszystkie encje
            foreach (var savedAlbumItem in allSavedAlbums)
            {
                var album = savedAlbumItem.Album;

                // Zbierz artystów albumu
                if (album.Artists != null)
                {
                    foreach (var artist in album.Artists)
                    {
                        if (!allArtists.ContainsKey(artist.Id))
                        {
                            allArtists[artist.Id] = artist;
                        }
                        allAlbumArtists.Add((album.Id, artist));
                    }
                }

                // Zbierz album
                if (!allAlbums.ContainsKey(album.Id))
                {
                    allAlbums[album.Id] = album;
                }

                // Zbierz utwory albumu
                if (album.Tracks?.Items != null)
                {
                    foreach (var track in album.Tracks.Items)
                    {
                        if (!allTracks.ContainsKey(track.Id))
                        {
                            allTracks[track.Id] = track;
                        }

                        // Zbierz artystów utworu
                        if (track.Artists != null)
                        {
                            foreach (var trackArtist in track.Artists)
                            {
                                allTrackArtists.Add((track.Id, trackArtist));

                                // Dodaj artystę utworu do ogólnej listy artystów
                                if (!allArtists.ContainsKey(trackArtist.Id))
                                {
                                    allArtists[trackArtist.Id] = trackArtist;
                                }
                            }
                        }
                    }
                }

                savedCount++;
            }

            // Krok 2: Zapisz artystów
            var artistsIds = new List<string>();

            foreach (var artist in allArtists.Values)
            {
                artistsIds.Add(artist.Id);
            }

            var existingArtists = context.SpArtists
                .Where(a => artistsIds.Contains(a.Id))
                .ToList();

            foreach (var artist in allArtists.Values)
            {
                var existingArtist = existingArtists.FirstOrDefault(a => a.Id == artist.Id);

                if (existingArtist is null)
                {
                    var newArtist = artist.MapToEntity(true);
                    context.SpArtists.Add(newArtist);
                    artistsCount++;
                }
            }

            await context.SaveChangesAsync();

            // Krok 3: Zapisz albumy
            var albumsIds = new List<string>();

            foreach (var album in allAlbums.Values)
            {
                albumsIds.Add(album.Id);
            }

            var existingAlbums = context.SpAlbums
                .Where(a => albumsIds.Contains(a.Id))
                .ToList();

            foreach (var album in allAlbums.Values)
            {
                var existingAlbum = existingAlbums.FirstOrDefault(a => a.Id == album.Id);

                if (existingAlbum == null)
                {
                    var newAlbum = album.MapToEntity(true);
                    context.SpAlbums.Add(newAlbum);
                    albumsCount++;
                }
                else
                {
                    // Aktualizuj istniejący album
                    var updatedAlbum = album.MapToEntity();
                    updatedAlbum.CreatedAt = existingAlbum.CreatedAt; // Zachowaj oryginalną datę utworzenia
                    context.Entry(existingAlbum).CurrentValues.SetValues(updatedAlbum);
                }
            }

            // Zapisz zmiany przed zapisaniem utworów
            await context.SaveChangesAsync();

            // Krok 4: Zapisz utwory (wersja zoptymalizowana)
            var totalTracks = allTracks.Values.ToList();
            var totalTracksCount = totalTracks.Count;
            var index = 1;

            // Map trackId -> albumId (jeśli dostępny)
            var trackAlbumIdMap = totalTracks
                .Where(t => t.Album != null && !string.IsNullOrEmpty(t.Album.Id))
                .ToDictionary(t => t.Id, t => t.Album!.Id, StringComparer.Ordinal);

            const int batchSize = 2000;

            // Wyłącz auto-wykrywanie zmian na czas masowych operacji
            var prevDetect = context.ChangeTracker.AutoDetectChangesEnabled;
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                while (totalTracks.Count > 0)
                {
                    var batch = totalTracks.Take(batchSize).ToList();
                    var batchIds = batch.Select(t => t.Id).ToList();

                    // Pobierz istniejące rekordy (tylko to co potrzebne) bez trackowania
                    var existing = await context.SpTracks
                        .AsNoTracking()
                        .Where(t => batchIds.Contains(t.Id))
                        .Select(t => new { t.Id, t.AlbumId })
                        .ToListAsync();

                    var existingIds = new HashSet<string>(existing.Select(e => e.Id), StringComparer.Ordinal);
                    var existingMissingAlbum = new HashSet<string>(
                        existing.Where(e => string.IsNullOrEmpty(e.AlbumId)).Select(e => e.Id),
                        StringComparer.Ordinal
                    );

                    // Inserty – tylko brakujące
                    var toAdd = new List<SpTrack>();

                    foreach (var track in batch)
                    {
                        if (index % 500 == 0)
                            Console.WriteLine($"Przeprocesowano {index}/{totalTracksCount} utworów");

                        if (!existingIds.Contains(track.Id))
                        {
                            var newTrack = track.MapToEntity(true);

                            if (track.Album != null)
                                newTrack.AlbumId = track.Album.Id;

                            toAdd.Add(newTrack);
                            tracksCount++;
                        }

                        index++;
                    }

                    if (toAdd.Count > 0)
                        context.SpTracks.AddRange(toAdd);

                    // Aktualizacje – ustaw tylko brakujące AlbumId (jeśli mamy je z API)
                    if (existingMissingAlbum.Count > 0)
                    {
                        foreach (var id in existingMissingAlbum)
                        {
                            if (trackAlbumIdMap.TryGetValue(id, out var albumId) && !string.IsNullOrEmpty(albumId))
                            {
                                var stub = new SpTrack { Id = id, AlbumId = albumId };
                                context.SpTracks.Attach(stub);
                                context.Entry(stub).Property(x => x.AlbumId).IsModified = true;
                            }
                        }
                    }

                    await context.SaveChangesAsync();

                    // Wyczyść tracker, aby nie puchł
                    context.ChangeTracker.Clear();

                    totalTracks.RemoveRange(0, Math.Min(batchSize, totalTracks.Count));
                }
            }
            finally
            {
                context.ChangeTracker.AutoDetectChangesEnabled = prevDetect;
            }

            // Krok 4: Zapisz relacje albumów z artystami
            var addedAlbumArtists = new HashSet<string>(); // AlbumId + ArtistId

            foreach (var (albumId, artist) in allAlbumArtists)
            {
                var albumArtistKey = $"{albumId}_{artist.Id}";

                if (!addedAlbumArtists.Contains(albumArtistKey))
                {
                    var existingAlbumArtist = await context.SpAlbumArtistAssigns
                        .FirstOrDefaultAsync(aaa => aaa.AlbumId == albumId && aaa.ArtistId == artist.Id);

                    if (existingAlbumArtist == null)
                    {
                        // Pobierz nazwę albumu z kolekcji allAlbums
                        var albumName = allAlbums.ContainsKey(albumId) ? allAlbums[albumId].Name : string.Empty;

                        var newAlbumArtist = artist.MapToAlbumArtistAssignEntity(albumId, albumName, true);

                        context.SpAlbumArtistAssigns.Add(newAlbumArtist);

                        addedAlbumArtists.Add(albumArtistKey);
                    }
                }
            }

            // Zapisz zmiany przed zapisaniem utworów
            await context.SaveChangesAsync();

            // Krok 5: Zapisz relacje artystów do utworów
            var addedTrackArtists = new HashSet<string>(); // TrackId + ArtistId

            index = 1;
            var totalRelations = allTrackArtists.Count;

            while (allTrackArtists.Count > 0)
            {
                var currentTrackArtistPortion = allTrackArtists.Take(2500).ToList();
                var currentTrackArtistTracksPortionsIds = currentTrackArtistPortion.Select(t => t.TrackId).ToList();

                var existingCurrentTrackArtistPortion = await context.SpTrackArtists
                        .Where(ta => currentTrackArtistTracksPortionsIds.Contains(ta.TrackId))
                        .ToListAsync();

                foreach (var (trackId, artist) in currentTrackArtistPortion)
                {
                    var trackArtistKey = $"{trackId}_{artist.Id}";

                    if (index % 500 == 0)
                        Console.WriteLine($"Przeprocesowano {index}/{totalRelations} relacji utwór-artysta");

                    if (!addedTrackArtists.Contains(trackArtistKey))
                    {
                        var existingTrackArtist = existingCurrentTrackArtistPortion
                            .FirstOrDefault(ta =>
                                ta.TrackId == trackId &&
                                ta.ArtistId == artist.Id
                            );

                        if (existingTrackArtist == null)
                        {
                            var newTrackArtist = artist.MapToTrackArtistEntity(trackId, true);
                            context.SpTrackArtists.Add(newTrackArtist);
                            addedTrackArtists.Add(trackArtistKey);
                        }
                    }

                    index++;
                }

                await context.SaveChangesAsync();

                allTrackArtists.RemoveRange(0, allTrackArtists.Count > 2500 ? 2500 : allTrackArtists.Count);
            }

            // Zapisz zmiany przed zapisaniem zapisanych albumów
            await context.SaveChangesAsync();

            // Krok 7: Zapisz zapisane albumy użytkownika
            var existingSavedAlbums = await context.SpSavedAlbums
                .Where(sa => sa.UserId == userId)
                .ToListAsync();

            foreach (var savedAlbum in allSavedAlbums)
            {
                var existingSavedAlbum = existingSavedAlbums
                    .FirstOrDefault(sa => sa.AlbumId == savedAlbum.Album.Id && sa.UserId == userId);

                if (existingSavedAlbum == null)
                {
                    var newSavedAlbum = savedAlbum.MapToEntity(userId);
                    context.SpSavedAlbums.Add(newSavedAlbum);
                }
            }

            await context.SaveChangesAsync();

            // Usuń zapisane albumy, które nie są już w bibliotece użytkownika
            foreach (var existingSavedAlbum in existingSavedAlbums)
            {
                var albumStillInLibrary = allSavedAlbums.Exists(sa => sa.Album.Id == existingSavedAlbum.AlbumId);

                if (!albumStillInLibrary)
                    context.SpSavedAlbums.Remove(existingSavedAlbum);
            }

            await context.SaveChangesAsync();

            // Krok 8: Zapisz przypisania albumów do użytkownika
            var existingAssigns = await context.LibAlbumUserAssigns
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var savedAlbum in allSavedAlbums)
            {
                var existingAssign = existingAssigns
                    .FirstOrDefault(a => a.AlbumId == savedAlbum.Album.Id && a.UserId == userId);

                if (existingAssign == null)
                {
                    var newAssign = savedAlbum.Album.MapToAlbumUserAssignEntity(userId);
                    context.LibAlbumUserAssigns.Add(newAssign);
                }
            }

            await context.SaveChangesAsync();

            // Usuń przypisania, które nie są już w bibliotece użytkownika
            foreach (var assign in existingAssigns)
            {
                var albumStillInLibrary = allSavedAlbums.Exists(sa => sa.Album.Id == assign.AlbumId);

                if (!albumStillInLibrary)
                    context.LibAlbumUserAssigns.Remove(assign);
            }

            await context.SaveChangesAsync();

            return new
            {
                message = "Synchronizacja zapisanych albumów zakończona pomyślnie",
                totalAlbums = savedCount,
                newArtists = artistsCount,
                newAlbums = albumsCount,
                newTracks = tracksCount
            };
        }

        public async Task<List<SpotifySavedAlbumItem>> GetAllUserSavedAlbumsParallel(
            int pageSize = 50,
            int maxDegreeOfParallelism = 6,
            CancellationToken ct = default)
        {
            // 1) Pierwsza strona – żeby poznać total
            var firstPage = await requestService.GetAsync<SpotifySavedAlbumsResponse>($"me/albums?limit={pageSize}&offset=0", ct);

            if (firstPage?.Items == null)
                return new List<SpotifySavedAlbumItem>();

            var total = firstPage.Total;
            var totalPages = (int)Math.Ceiling((double)total / pageSize);

            if (totalPages <= 1)
                return firstPage.Items;

            // 2) Równoległe pobieranie pozostałych stron
            var pages = new ConcurrentDictionary<int, List<SpotifySavedAlbumItem>>();
            pages[0] = firstPage.Items;

            var offsets = Enumerable.Range(1, totalPages - 1)
                .Select(i => (pageIndex: i, offset: i * pageSize))
                .ToList();

            using var sem = new SemaphoreSlim(maxDegreeOfParallelism);

            var tasks = offsets.Select(async x =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var page = await requestService.GetAsync<SpotifySavedAlbumsResponse>($"me/albums?limit={pageSize}&offset={x.offset}", ct);

                    if (page?.Items != null)
                    {
                        pages[x.pageIndex] = page.Items;
                    }
                }
                finally
                {
                    sem.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            // 3) Sklej w kolejności
            var result = new List<SpotifySavedAlbumItem>(capacity: total);

            for (int i = 0; i < totalPages; i++)
            {
                if (pages.TryGetValue(i, out var items) && items != null)
                    result.AddRange(items);
            }

            return result;
        }

        public async Task<object> AddArtistAlbumsToUserLibraryAsync(string artistId)
        {
            var albums = await GetArtistAlbumsAsync(artistId, includeGroups: "album");
            var distinctIds = albums.Select(a => a.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var toAdd = new List<string>();

            for (int i = 0; i < distinctIds.Count; i += 50)
            {
                var chunk = distinctIds.Skip(i).Take(50).ToList();
                var idsCsv = string.Join(",", chunk);

                var containsRespObj = await requestService.MakeSpotifyRequestAsync($"me/albums/contains?ids={idsCsv}");

                if (containsRespObj == null)
                    throw new InvalidOperationException("Błąd podczas sprawdzania, które albumy są w bibliotece użytkownika.");

                var containsArr = JsonSerializer.Deserialize<bool[]>(containsRespObj.ToString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Array.Empty<bool>();

                for (int j = 0; j < chunk.Count; j++)
                {
                    if (j >= containsArr.Length || !containsArr[j])
                        toAdd.Add(chunk[j]);
                }
            }

            var addedCount = 0;

            for (int i = 0; i < toAdd.Count; i += 50)
            {
                var chunk = toAdd.Skip(i).Take(50).ToList();
                var idsCsv = string.Join(",", chunk);

                var ok = await requestService.SendAsync(HttpMethod.Put, $"me/albums?ids={idsCsv}");

                if (!ok)
                    throw new InvalidOperationException("Nie udało się dodać niektórych albumów do biblioteki użytkownika.");

                addedCount += chunk.Count;
            }

            return new
            {
                totalFound = distinctIds.Count,
                alreadyInLibrary = distinctIds.Count - toAdd.Count,
                added = addedCount
            };
        }

        #endregion Albums

        #region Tracks

        public async Task<SpotifySavedTracksResponse?> GetUserSavedTracks(int limit = 50)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"me/tracks?limit={limit}");

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifySavedTracksResponse>(response.ToString()!);
        }

        public async Task<List<SpotifySavedTrackItem>> GetAllUserSavedTracks()
        {
            var allSavedTracks = new List<SpotifySavedTrackItem>();
            string? next = null;
            const int limit = 50;

            do
            {
                var endpoint = $"me/tracks?limit={limit}";

                if (!string.IsNullOrEmpty(next))
                {
                    endpoint = next;
                }

                var response = await requestService.MakeSpotifyRequestAsync(endpoint);

                if (response == null)
                    break;

                var savedTracksResponse = JsonSerializer.Deserialize<SpotifySavedTracksResponse>(response.ToString()!);

                if (savedTracksResponse?.Items == null)
                    break;

                allSavedTracks.AddRange(savedTracksResponse.Items);
                next = savedTracksResponse.Next;
            } while (!string.IsNullOrEmpty(next));

            return allSavedTracks;
        }

        public async Task<object> SyncUserSavedTracks()
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
                throw new InvalidOperationException("Nie można pobrać profilu użytkownika");

            var userId = userProfile.Id;
            var allSavedTracks = await GetAllUserSavedTracks();

            if (allSavedTracks == null || !allSavedTracks.Any())
                return new { message = "Brak zapisanych utworów do synchronizacji", savedCount = 0 };

            var savedCount = 0;
            var artistsCount = 0;
            var albumsCount = 0;
            var tracksCount = 0;

            // Kolekcje do zbierania wszystkich encji przed zapisaniem
            var allArtists = new Dictionary<string, SpotifyArtistObject>();
            var allAlbums = new Dictionary<string, SpotifyAlbumObject>();
            var allTracks = new Dictionary<string, SpotifyTrackObject>();
            var allTrackArtists = new List<(string TrackId, SpotifyArtistObject Artist)>();

            // Krok 1: Zbierz wszystkie encje
            foreach (var savedTrackItem in allSavedTracks)
            {
                var track = savedTrackItem.Track;

                // Zbierz artystów utworu
                if (track.Artists != null)
                {
                    foreach (var artist in track.Artists)
                    {
                        if (!allArtists.ContainsKey(artist.Id))
                        {
                            allArtists[artist.Id] = artist;
                        }
                        allTrackArtists.Add((track.Id, artist));
                    }
                }

                // Zbierz album utworu
                if (track.Album != null && !allAlbums.ContainsKey(track.Album.Id))
                {
                    allAlbums[track.Album.Id] = track.Album;
                }

                // Zbierz utwór
                if (!allTracks.ContainsKey(track.Id))
                {
                    allTracks[track.Id] = track;
                }

                savedCount++;
            }

            // Krok 2: Zapisz artystów
            foreach (var artist in allArtists.Values)
            {
                var existingArtist = await context.SpArtists.FirstOrDefaultAsync(a => a.Id == artist.Id);
                if (existingArtist == null)
                {
                    var newArtist = artist.MapToEntity(true);
                    context.SpArtists.Add(newArtist);
                    artistsCount++;
                }
            }

            // Krok 3: Zapisz albumy
            foreach (var album in allAlbums.Values)
            {
                var existingAlbum = await context.SpAlbums.FirstOrDefaultAsync(a => a.Id == album.Id);
                if (existingAlbum == null)
                {
                    var newAlbum = album.MapToEntity(true);
                    context.SpAlbums.Add(newAlbum);
                    albumsCount++;
                }
                else
                {
                    // Aktualizuj istniejący album
                    var updatedAlbum = album.MapToEntity();
                    updatedAlbum.CreatedAt = existingAlbum.CreatedAt; // Zachowaj oryginalną datę utworzenia
                    context.Entry(existingAlbum).CurrentValues.SetValues(updatedAlbum);
                }
            }

            // Zapisz zmiany przed zapisaniem utworów
            await context.SaveChangesAsync();

            // Krok 4: Zapisz utwory
            foreach (var track in allTracks.Values)
            {
                var existingTrack = await context.SpTracks.FirstOrDefaultAsync(t => t.Id == track.Id);
                if (existingTrack == null)
                {
                    var newTrack = track.MapToEntity(true);
                    // Ustaw album dla tego utworu
                    if (track.Album != null)
                    {
                        newTrack.AlbumId = track.Album.Id;
                    }
                    context.SpTracks.Add(newTrack);
                    tracksCount++;
                }
                else
                {
                    // Aktualizuj istniejący utwór, szczególnie AlbumId jeśli jest null
                    var updatedTrack = track.MapToEntity();
                    updatedTrack.CreatedAt = existingTrack.CreatedAt; // Zachowaj oryginalną datę utworzenia

                    // Jeśli istniejący utwór nie ma AlbumId, ale nowy ma album, ustaw AlbumId
                    if (string.IsNullOrEmpty(existingTrack.AlbumId) && track.Album != null)
                    {
                        updatedTrack.AlbumId = track.Album.Id;
                    }
                    else if (!string.IsNullOrEmpty(existingTrack.AlbumId))
                    {
                        // Zachowaj istniejący AlbumId jeśli już jest ustawiony
                        updatedTrack.AlbumId = existingTrack.AlbumId;
                    }

                    context.Entry(existingTrack).CurrentValues.SetValues(updatedTrack);
                }
            }

            // Zapisz zmiany przed tworzeniem relacji
            await context.SaveChangesAsync();

            // Krok 5: Zapisz relacje artystów do utworów
            var addedTrackArtists = new HashSet<string>(); // TrackId + ArtistId
            foreach (var (trackId, artist) in allTrackArtists)
            {
                var trackArtistKey = $"{trackId}_{artist.Id}";
                if (!addedTrackArtists.Contains(trackArtistKey))
                {
                    var existingTrackArtist = await context.SpTrackArtists
                        .FirstOrDefaultAsync(ta => ta.TrackId == trackId && ta.ArtistId == artist.Id);

                    if (existingTrackArtist == null)
                    {
                        var newTrackArtist = artist.MapToTrackArtistEntity(trackId, true);
                        context.SpTrackArtists.Add(newTrackArtist);
                        addedTrackArtists.Add(trackArtistKey);
                    }
                }
            }

            // Zapisz zmiany przed zapisaniem zapisanych utworów
            await context.SaveChangesAsync();

            // Krok 6: Zapisz zapisane utwory użytkownika
            var existingSavedTracks = await context.SpSavedTracks
                .Where(st => st.UserId == userId)
                .ToListAsync();

            foreach (var savedTrack in allSavedTracks)
            {
                var existingSavedTrack = existingSavedTracks
                    .FirstOrDefault(st => st.TrackId == savedTrack.Track.Id && st.UserId == userId);

                if (existingSavedTrack == null)
                {
                    var newSavedTrack = new SpSavedTrack
                    {
                        UserId = userId,
                        TrackId = savedTrack.Track.Id,
                        AddedAt = DateTime.Parse(savedTrack.AddedAt),
                        CreatedAt = DateTime.UtcNow
                    };
                    context.SpSavedTracks.Add(newSavedTrack);
                }
            }

            // Usuń zapisane utwory, które nie są już w bibliotece użytkownika
            foreach (var existingSavedTrack in existingSavedTracks)
            {
                var trackStillInLibrary = allSavedTracks.Exists(st => st.Track.Id == existingSavedTrack.TrackId);

                if (!trackStillInLibrary)
                    context.SpSavedTracks.Remove(existingSavedTrack);
            }

            // Krok 7: Zapisz przypisania utworów do użytkownika
            var existingAssigns = await context.LibTrackUserAssigns
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var savedTrack in allSavedTracks)
            {
                var existingAssign = existingAssigns
                    .FirstOrDefault(a => a.TrackId == savedTrack.Track.Id && a.UserId == userId);

                if (existingAssign == null)
                {
                    var newAssign = savedTrack.Track.MapToTrackUserAssignEntity(userId);
                    context.LibTrackUserAssigns.Add(newAssign);
                }
            }

            // Usuń przypisania, które nie są już w bibliotece użytkownika
            foreach (var assign in existingAssigns)
            {
                var trackStillInLibrary = allSavedTracks.Exists(st => st.Track.Id == assign.TrackId);

                if (!trackStillInLibrary)
                    context.LibTrackUserAssigns.Remove(assign);
            }

            await context.SaveChangesAsync();

            return new
            {
                message = "Synchronizacja zapisanych utworów zakończona pomyślnie",
                totalTracks = savedCount,
                newArtists = artistsCount,
                newAlbums = albumsCount,
                newTracks = tracksCount
            };
        }

        #endregion Tracks

        #region Playlists

        public async Task<SpotifyPlaylistsResponse?> GetUserPlaylists(int limit = 50)
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
                return null;

            var url = $"https://api.spotify.com/v1/users/{userProfile.Id}/playlists?limit={limit}";
            var response = await requestService.MakeSpotifyRequestAsync(url);

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyPlaylistsResponse>(response.ToString()!);
        }

        public async Task<List<SpotifyPlaylistObject>> GetAllUserPlaylists()
        {
            var allPlaylists = new List<SpotifyPlaylistObject>();
            var limit = 50;
            var offset = 0;

            while (true)
            {
                var userProfile = await GetCurrentUserProfileAsync();
                if (userProfile == null)
                    break;

                var url = $"https://api.spotify.com/v1/users/{userProfile.Id}/playlists?limit={limit}&offset={offset}";
                var response = await requestService.MakeSpotifyRequestAsync(url);

                if (response == null)
                    break;

                var playlistsResponse = JsonSerializer.Deserialize<SpotifyPlaylistsResponse>(response.ToString()!);
                if (playlistsResponse?.Items == null || !playlistsResponse.Items.Any())
                    break;

                allPlaylists.AddRange(playlistsResponse.Items);

                if (string.IsNullOrEmpty(playlistsResponse.Next))
                    break;

                offset += limit;
            }

            return allPlaylists;
        }

        public async Task<SpotifyPlaylistTracksResponse?> GetPlaylistTracks(string playlistId, int limit = 50)
        {
            // Limit 50 jest maksymalnym limitem dla endpointu /playlists/{playlist_id}/tracks
            // zgodnie z dokumentacją Spotify API
            var url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit={limit}";
            var response = await requestService.MakeSpotifyRequestAsync(url);

            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyPlaylistTracksResponse>(response.ToString()!);
        }

        public async Task<List<SpotifyPlaylistTrackItem>> GetAllPlaylistTracks(string playlistId)
        {
            var allTracks = new List<SpotifyPlaylistTrackItem>();
            var limit = 50; // Maksymalny limit zgodnie z dokumentacją Spotify API
            var offset = 0;

            while (true)
            {
                var url = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit={limit}&offset={offset}";
                var response = await requestService.MakeSpotifyRequestAsync(url);

                if (response == null)
                    break;

                var tracksResponse = JsonSerializer.Deserialize<SpotifyPlaylistTracksResponse>(response.ToString()!);
                if (tracksResponse?.Items == null || !tracksResponse.Items.Any())
                    break;

                allTracks.AddRange(tracksResponse.Items);

                if (string.IsNullOrEmpty(tracksResponse.Next))
                    break;

                offset += limit;
            }

            return allTracks;
        }

        public async Task<object> SyncUserPlaylists()
        {
            var userProfile = await GetCurrentUserProfileAsync();
            if (userProfile == null)
                throw new InvalidOperationException("Nie można pobrać profilu użytkownika");

            var userId = userProfile.Id;
            var allPlaylists = await GetAllUserPlaylists();

            if (allPlaylists == null || !allPlaylists.Any())
                return new { message = "Brak playlist do synchronizacji", playlistsCount = 0 };

            var playlistsCount = 0;
            var artistsCount = 0;
            var albumsCount = 0;
            var tracksCount = 0;
            var playlistTracksCount = 0;

            // Kolekcje do zbierania wszystkich encji przed zapisaniem
            var allArtists = new Dictionary<string, SpotifyArtistObject>();
            var allAlbums = new Dictionary<string, SpotifyAlbumObject>();
            var allTracks = new Dictionary<string, SpotifyTrackObject>();
            var allTrackArtists = new List<(string TrackId, SpotifyArtistObject Artist)>();
            var allPlaylistTracks = new List<(string PlaylistId, SpotifyPlaylistTrackItem TrackItem)>();
            var allPlaylistOwners = new Dictionary<string, SpotifyUserProfile>();

            // Krok 0: Pobierz istniejące ID z bazy danych (optymalizacja)
            var existingArtistIds = (await context.SpArtists.Select(a => a.Id).ToListAsync()).ToHashSet();
            var existingAlbumIds = (await context.SpAlbums.Select(a => a.Id).ToListAsync()).ToHashSet();
            var existingTrackIds = (await context.SpTracks.Select(t => t.Id).ToListAsync()).ToHashSet();
            var existingUserIds = (await context.SpUsers.Select(u => u.Id).ToListAsync()).ToHashSet();
            var existingTrackArtistKeys = (await context.SpTrackArtists
                .Select(ta => $"{ta.TrackId}_{ta.ArtistId}")
                .ToListAsync()).ToHashSet();

            // Pobierz istniejące encje do aktualizacji (optymalizacja)
            var existingArtists = await context.SpArtists
                .Where(a => allArtists.Keys.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);
            var existingAlbums = await context.SpAlbums
                .Where(a => allAlbums.Keys.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);
            var existingTracks = await context.SpTracks
                .Where(t => allTracks.Keys.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);
            var existingUsers = await context.SpUsers
                .Where(u => allPlaylistOwners.Keys.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // Krok 1: Zbierz wszystkie playlisty i ich utwory
            foreach (var playlist in allPlaylists)
            {
                var playlistTracks = await GetAllPlaylistTracks(playlist.Id);

                foreach (var trackItem in playlistTracks)
                {
                    if (trackItem.Track == null)
                        continue;

                    var track = trackItem.Track;

                    // Zbierz artystów utworu
                    if (track.Artists != null)
                    {
                        foreach (var artist in track.Artists)
                        {
                            if (!allArtists.ContainsKey(artist.Id))
                            {
                                allArtists[artist.Id] = artist;
                            }
                            allTrackArtists.Add((track.Id, artist));
                        }
                    }

                    // Zbierz album utworu
                    if (track.Album != null && !allAlbums.ContainsKey(track.Album.Id))
                    {
                        allAlbums[track.Album.Id] = track.Album;
                    }

                    // Zbierz utwór
                    if (!allTracks.ContainsKey(track.Id))
                    {
                        allTracks[track.Id] = track;
                    }

                    // Zbierz utwór playlisty
                    allPlaylistTracks.Add((playlist.Id, trackItem));
                }

                // Zbierz właściciela playlisty
                if (!allPlaylistOwners.ContainsKey(playlist.Owner.Id))
                {
                    allPlaylistOwners[playlist.Owner.Id] = playlist.Owner;
                }

                playlistsCount++;
            }

            // Krok 2: Zapisz artystów
            foreach (var artist in allArtists.Values)
            {
                if (!existingArtistIds.Contains(artist.Id))
                {
                    var newArtist = artist.MapToEntity(true);
                    context.SpArtists.Add(newArtist);
                    existingArtistIds.Add(artist.Id); // Aktualizuj HashSet
                    artistsCount++;
                }
                else
                {
                    // Aktualizuj istniejący artysta
                    if (existingArtists.TryGetValue(artist.Id, out var existingArtist))
                    {
                        var updatedArtist = artist.MapToEntity();
                        updatedArtist.CreatedAt = existingArtist.CreatedAt;
                        context.Entry(existingArtist).CurrentValues.SetValues(updatedArtist);
                    }
                }
            }

            // Krok 3: Zapisz albumy
            foreach (var album in allAlbums.Values)
            {
                if (!existingAlbumIds.Contains(album.Id))
                {
                    var newAlbum = album.MapToEntity(true);
                    context.SpAlbums.Add(newAlbum);
                    existingAlbumIds.Add(album.Id); // Aktualizuj HashSet
                    albumsCount++;
                }
                else
                {
                    // Aktualizuj istniejący album
                    if (existingAlbums.TryGetValue(album.Id, out var existingAlbum))
                    {
                        var updatedAlbum = album.MapToEntity();
                        updatedAlbum.CreatedAt = existingAlbum.CreatedAt;
                        context.Entry(existingAlbum).CurrentValues.SetValues(updatedAlbum);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Krok 4: Zapisz utwory
            foreach (var track in allTracks.Values)
            {
                if (!existingTrackIds.Contains(track.Id))
                {
                    var newTrack = track.MapToEntity(true);
                    if (track.Album != null)
                    {
                        newTrack.AlbumId = track.Album.Id;
                    }
                    context.SpTracks.Add(newTrack);
                    existingTrackIds.Add(track.Id); // Aktualizuj HashSet
                    tracksCount++;
                }
                else
                {
                    // Aktualizuj istniejący utwór
                    if (existingTracks.TryGetValue(track.Id, out var existingTrack))
                    {
                        var updatedTrack = track.MapToEntity();
                        updatedTrack.CreatedAt = existingTrack.CreatedAt;

                        if (string.IsNullOrEmpty(existingTrack.AlbumId) && track.Album != null)
                        {
                            updatedTrack.AlbumId = track.Album.Id;
                        }
                        else if (!string.IsNullOrEmpty(existingTrack.AlbumId))
                        {
                            updatedTrack.AlbumId = existingTrack.AlbumId;
                        }

                        context.Entry(existingTrack).CurrentValues.SetValues(updatedTrack);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Krok 5: Zapisz właścicieli playlist
            foreach (var owner in allPlaylistOwners.Values)
            {
                if (!existingUserIds.Contains(owner.Id))
                {
                    var newOwner = owner.MapToEntity(true);
                    context.SpUsers.Add(newOwner);
                    existingUserIds.Add(owner.Id); // Aktualizuj HashSet
                }
                else
                {
                    // Aktualizuj istniejącego właściciela
                    if (existingUsers.TryGetValue(owner.Id, out var existingOwner))
                    {
                        var updatedOwner = owner.MapToEntity();
                        updatedOwner.CreatedAt = existingOwner.CreatedAt;
                        context.Entry(existingOwner).CurrentValues.SetValues(updatedOwner);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Krok 6: Zapisz playlisty
            foreach (var playlist in allPlaylists)
            {
                var existingPlaylist = await context.SpPlaylists.FirstOrDefaultAsync(p => p.Id == playlist.Id);
                if (existingPlaylist == null)
                {
                    var newPlaylist = playlist.MapToEntity(true);
                    context.SpPlaylists.Add(newPlaylist);
                }
                else
                {
                    // Aktualizuj istniejącą playlistę
                    var updatedPlaylist = playlist.MapToEntity();
                    updatedPlaylist.CreatedAt = existingPlaylist.CreatedAt;
                    context.Entry(existingPlaylist).CurrentValues.SetValues(updatedPlaylist);
                }
            }

            await context.SaveChangesAsync();

            // Krok 7: Zapisz relacje artystów do utworów
            var addedTrackArtists = new HashSet<string>();
            foreach (var (trackId, artist) in allTrackArtists)
            {
                var trackArtistKey = $"{trackId}_{artist.Id}";
                if (!addedTrackArtists.Contains(trackArtistKey))
                {
                    if (!existingTrackArtistKeys.Contains(trackArtistKey))
                    {
                        var newTrackArtist = artist.MapToTrackArtistEntity(trackId, true);
                        context.SpTrackArtists.Add(newTrackArtist);
                        existingTrackArtistKeys.Add(trackArtistKey); // Aktualizuj HashSet
                        addedTrackArtists.Add(trackArtistKey);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Krok 8: Zapisz utwory playlist
            var existingPlaylistTrackKeys = (await context.SpPlaylistTracks
                .Where(pt => allPlaylists.Select(p => p.Id).Contains(pt.PlaylistId))
                .Select(pt => $"{pt.PlaylistId}_{pt.TrackId}")
                .ToListAsync()).ToHashSet();

            foreach (var (playlistId, trackItem) in allPlaylistTracks)
            {
                if (trackItem.Track == null)
                    continue;

                var playlistTrackKey = $"{playlistId}_{trackItem.Track.Id}";
                if (!existingPlaylistTrackKeys.Contains(playlistTrackKey))
                {
                    var newPlaylistTrack = trackItem.MapToPlaylistTrackEntity(playlistId);
                    context.SpPlaylistTracks.Add(newPlaylistTrack);
                    existingPlaylistTrackKeys.Add(playlistTrackKey); // Aktualizuj HashSet
                    playlistTracksCount++;
                }
            }

            // Usuń utwory playlist, które nie są już w playlistach
            // Pobierz wszystkie istniejące utwory playlist do usunięcia
            var existingPlaylistTracksToRemove = await context.SpPlaylistTracks
                .Where(pt => allPlaylists.Select(p => p.Id).Contains(pt.PlaylistId))
                .ToListAsync();

            foreach (var existingPlaylistTrack in existingPlaylistTracksToRemove)
            {
                var trackStillInPlaylist = allPlaylistTracks.Exists(pt =>
                    pt.PlaylistId == existingPlaylistTrack.PlaylistId &&
                    pt.TrackItem.Track?.Id == existingPlaylistTrack.TrackId);

                if (!trackStillInPlaylist)
                    context.SpPlaylistTracks.Remove(existingPlaylistTrack);
            }

            await context.SaveChangesAsync();

            // Usuń playlisty tego użytkownika, które nie istnieją już na Spotify
            var spotifyPlaylistIds = allPlaylists.Select(p => p.Id).ToHashSet();
            var playlistsToDelete = await context.SpPlaylists
                .Where(p => p.OwnerId == userId && !spotifyPlaylistIds.Contains(p.Id))
                .ToListAsync();

            var removedPlaylists = playlistsToDelete.Count;

            if (removedPlaylists > 0)

                context.SpPlaylists.RemoveRange(playlistsToDelete);

            await context.SaveChangesAsync();

            return new
            {
                message = "Synchronizacja playlist zakończona pomyślnie",
                totalPlaylists = playlistsCount,
                newArtists = artistsCount,
                newAlbums = albumsCount,
                newTracks = tracksCount,
                newPlaylistTracks = playlistTracksCount,
                removedPlaylists
            };
        }

        #endregion Playlists

        public async Task<bool> UnfollowPlaylistAsync(string playlistId)
        {
            if (string.IsNullOrWhiteSpace(playlistId))

                return false;

            var ok = await requestService.SendAsync(HttpMethod.Delete, $"playlists/{playlistId}/followers");

            if (!ok)
            {
                // Jeśli Spotify zwrócił błąd (np. 404, bo już nie ma), sprawdź istnienie playlisty – brak traktujemy jako sukces
                var exists = await requestService.MakeSpotifyRequestAsync($"playlists/{playlistId}");
                if (exists == null)
                    return true;
            }

            return ok;
        }

        public async Task<SpotifyPlaylistObject?> GetPlaylistAsync(string playlistId)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"playlists/{playlistId}");

            if (response == null)

                return null;

            return JsonSerializer.Deserialize<SpotifyPlaylistObject>(response.ToString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> DeleteOrUnfollowPlaylistAsync(string playlistId)
        {
            var user = await GetCurrentUserProfileAsync();
            var userId = user?.Id;

            if (string.IsNullOrWhiteSpace(userId))

                return false;

            var playlist = await GetPlaylistAsync(playlistId);

            if (playlist == null)

                return true; // już nie istnieje

            var isOwner = string.Equals(playlist.Owner?.Id, userId, StringComparison.Ordinal);

            var ok = await requestService.SendAsync(HttpMethod.Delete, $"playlists/{playlistId}/followers");

            if (!ok)
            {
                // Sprawdź faktyczny stan po operacji
                var stillExists = await GetPlaylistAsync(playlistId);

                if (stillExists == null)

                    return true; // usunięto lub odsubowano

                // Dla właściciela żądanie powinno usuwać playlistę; jeśli nadal istnieje, raportuj błąd
                return false;
            }

            return true;
        }
    }
}