using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Helpers;
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

        // Przykład zapisywania użytkownika z obrazami
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

        // Przykład wyszukiwania artystów po gatunku
        public async Task<List<SpArtist>> GetArtistsByGenreAsync(string genre)
        {
            var artists = await _context.SpArtists
                .Where(a => a.Genres != null && a.Genres.Contains(genre))
                .ToListAsync();

            return artists;
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
    }
}
