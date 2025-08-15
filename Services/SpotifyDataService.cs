using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Helpers;
using SpotifyBasicApi2025.Models;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyDataService
    {
        private readonly DatabaseContext _context;

        public SpotifyDataService(DatabaseContext context)
        {
            _context = context;
        }

        // Przykład zapisywania artysty z listą gatunków
        public async Task<SpArtist> SaveArtistAsync(string spotifyId, string name, List<string> genres, List<string> images)
        {
            var artist = new SpArtist
            {
                Id = spotifyId,
                Name = name,
                Genres = StringListConverter.ListToString(genres),
                Images = StringListConverter.ListToString(images),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpArtists.Add(artist);
            await _context.SaveChangesAsync();

            return artist;
        }

        // Przykład pobierania artysty z konwersją list
        public async Task<SpArtist?> GetArtistAsync(string spotifyId)
        {
            return await _context.SpArtists
                .FirstOrDefaultAsync(a => a.Id == spotifyId);
        }

        // Przykład pobierania gatunków artysty
        public async Task<List<string>> GetArtistGenresAsync(string spotifyId)
        {
            var artist = await _context.SpArtists
                .FirstOrDefaultAsync(a => a.Id == spotifyId);

            if (artist == null)
                return new List<string>();

            return StringListConverter.StringToList(artist.Genres);
        }

        // Przykład zapisywania albumu z rynkami
        public async Task<SpAlbum> SaveAlbumAsync(string spotifyId, string name, string artistId, List<string> availableMarkets, List<string> images)
        {
            var album = new SpAlbum
            {
                Id = spotifyId,
                Name = name,
                ArtistId = artistId,
                AvailableMarkets = StringListConverter.ListToString(availableMarkets),
                Images = StringListConverter.ListToString(images),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpAlbums.Add(album);
            await _context.SaveChangesAsync();

            return album;
        }

        // Przykład zapisywania utworu z rynkami
        public async Task<SpTrack> SaveTrackAsync(string spotifyId, string name, string albumId, List<string> availableMarkets, List<string> images)
        {
            var track = new SpTrack
            {
                Id = spotifyId,
                Name = name,
                AlbumId = albumId,
                AvailableMarkets = StringListConverter.ListToString(availableMarkets),
                Images = StringListConverter.ListToString(images),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpTracks.Add(track);
            await _context.SaveChangesAsync();

            return track;
        }

        // Przykład zapisywania playlisty z obrazami
        public async Task<SpPlaylist> SavePlaylistAsync(string spotifyId, string name, string ownerId, List<string> images)
        {
            var playlist = new SpPlaylist
            {
                Id = spotifyId,
                Name = name,
                OwnerId = ownerId,
                Images = StringListConverter.ListToString(images),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpPlaylists.Add(playlist);
            await _context.SaveChangesAsync();

            return playlist;
        }

        // Zapisywanie użytkownika z profilu Spotify
        public async Task<SpUser> SaveUserFromSpotifyProfileAsync(SpotifyUserProfile spotifyProfile)
        {
            // Sprawdź czy użytkownik już istnieje
            var existingUser = await _context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == spotifyProfile.Id);

            if (existingUser != null)
            {
                // Aktualizuj istniejącego użytkownika
                existingUser.DisplayName = spotifyProfile.DisplayName;
                existingUser.Type = spotifyProfile.Type;
                existingUser.Uri = spotifyProfile.Uri;
                existingUser.Href = spotifyProfile.Href;
                existingUser.Country = spotifyProfile.Country;
                existingUser.Email = spotifyProfile.Email;
                existingUser.Product = spotifyProfile.Product;
                existingUser.Followers = spotifyProfile.Followers.Total;
                existingUser.Images = StringListConverter.ListToString(spotifyProfile.Images.Select(img => img.Url).ToList());
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingUser;
            }
            else
            {
                // Dodaj nowego użytkownika
                var newUser = new SpUser
                {
                    Id = spotifyProfile.Id,
                    DisplayName = spotifyProfile.DisplayName,
                    Type = spotifyProfile.Type,
                    Uri = spotifyProfile.Uri,
                    Href = spotifyProfile.Href,
                    Country = spotifyProfile.Country,
                    Email = spotifyProfile.Email,
                    Product = spotifyProfile.Product,
                    Followers = spotifyProfile.Followers.Total,
                    Images = StringListConverter.ListToString(spotifyProfile.Images.Select(img => img.Url).ToList()),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SpUsers.Add(newUser);
                await _context.SaveChangesAsync();

                return newUser;
            }
        }

        // Przykład zapisywania użytkownika z obrazami (zachowany dla kompatybilności)
        public async Task<SpUser> SaveUserAsync(string spotifyId, string displayName, List<string> images)
        {
            var user = new SpUser
            {
                Id = spotifyId,
                DisplayName = displayName,
                Images = StringListConverter.ListToString(images),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpUsers.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        // Przykład pobierania wszystkich obrazów artysty
        public async Task<List<string>> GetArtistImagesAsync(string spotifyId)
        {
            var artist = await _context.SpArtists
                .FirstOrDefaultAsync(a => a.Id == spotifyId);

            if (artist == null)
                return new List<string>();

            return StringListConverter.StringToList(artist.Images);
        }

        // Zapisywanie śledzonych artystów z Spotify API
        public async Task<int> SaveFollowedArtistsAsync(List<SpotifyArtistObject> spotifyArtists)
        {
            var savedCount = 0;

            foreach (var spotifyArtist in spotifyArtists)
            {
                // Sprawdź czy artysta już istnieje
                var existingArtist = await _context.SpArtists
                    .FirstOrDefaultAsync(a => a.Id == spotifyArtist.Id);

                if (existingArtist != null)
                {
                    // Aktualizuj istniejącego artystę
                    existingArtist.Name = spotifyArtist.Name;
                    existingArtist.Type = spotifyArtist.Type;
                    existingArtist.Uri = spotifyArtist.Uri;
                    existingArtist.Href = spotifyArtist.Href;
                    existingArtist.Genres = StringListConverter.ListToString(spotifyArtist.Genres);
                    existingArtist.Popularity = spotifyArtist.Popularity;
                    existingArtist.Images = StringListConverter.ListToString(spotifyArtist.Images.Select(img => img.Url).ToList());
                    existingArtist.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Dodaj nowego artystę
                    var newArtist = new SpArtist
                    {
                        Id = spotifyArtist.Id,
                        Name = spotifyArtist.Name,
                        Type = spotifyArtist.Type,
                        Uri = spotifyArtist.Uri,
                        Href = spotifyArtist.Href,
                        Genres = StringListConverter.ListToString(spotifyArtist.Genres),
                        Popularity = spotifyArtist.Popularity,
                        Images = StringListConverter.ListToString(spotifyArtist.Images.Select(img => img.Url).ToList()),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SpArtists.Add(newArtist);
                }

                savedCount++;
            }

            await _context.SaveChangesAsync();
            return savedCount;
        }

        // Pobieranie wszystkich artystów z bazy danych
        public async Task<List<SpArtist>> GetAllArtistsAsync()
        {
            return await _context.SpArtists
                .OrderByDescending(a => a.Popularity)
                .ToListAsync();
        }

        // Pobieranie artystów po gatunku
        public async Task<List<SpArtist>> GetArtistsByGenreAsync(string genre)
        {
            return await _context.SpArtists
                .Where(a => a.Genres != null && a.Genres.Contains(genre))
                .OrderByDescending(a => a.Popularity)
                .ToListAsync();
        }

        // Pobieranie artystów o najwyższej popularności
        public async Task<List<SpArtist>> GetTopArtistsAsync(int limit = 10)
        {
            return await _context.SpArtists
                .OrderByDescending(a => a.Popularity)
                .Take(limit)
                .ToListAsync();
        }

        // Pobieranie użytkownika po ID
        public async Task<SpUser?> GetUserByIdAsync(string userId)
        {
            return await _context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // Pobieranie wszystkich użytkowników
        public async Task<List<SpUser>> GetAllUsersAsync()
        {
            return await _context.SpUsers
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }
    }
}