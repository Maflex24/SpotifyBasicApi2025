using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;
using System.Text.Json;

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
    }
}