using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Helpers;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace SpotifyBasicApi2025.Models.Extensions
{
    public static class SpotifyMappingExtensions
    {
        public static SpArtist MapToEntity(this SpotifyArtistObject model, bool newEntity = false)
        {
            var entity = new SpArtist
            {
                Id = model.Id,
                Name = model.Name,
                Type = model.Type,
                Uri = model.Uri,
                Href = model.Href,
                Genres = StringListConverter.ListToString(model.Genres),
                Popularity = model.Popularity,
                Images = model.Images.ToJsonCompact(),
                UpdatedAt = DateTime.UtcNow
            };

            if (newEntity)
                entity.CreatedAt = DateTime.UtcNow;

            return entity;
        }

        public static SpAlbum MapToEntity(this SpotifyAlbumObject model, bool newEntity = false)
        {
            var entity = new SpAlbum
            {
                Id = model.Id,
                Name = model.Name,
                Type = model.Type,
                Uri = model.Uri,
                Href = model.Href,
                AlbumType = model.AlbumType,
                TotalTracks = model.TotalTracks,
                AvailableMarkets = StringListConverter.ListToString(model.AvailableMarkets),
                ReleaseDate = model.ReleaseDate,
                ReleaseDatePrecision = model.ReleaseDatePrecision,
                Images = model.Images.ToJsonCompact(),
                Popularity = model.Popularity,
                Label = model.Label,
                UpdatedAt = DateTime.UtcNow
            };

            if (newEntity)
                entity.CreatedAt = DateTime.UtcNow;

            return entity;
        }

        public static SpTrack MapToEntity(this SpotifyTrackObject model, bool newEntity = false)
        {
            var entity = new SpTrack
            {
                Id = model.Id,
                Name = model.Name,
                Type = model.Type,
                Uri = model.Uri,
                Href = model.Href,
                DurationMs = model.DurationMs,
                Explicit = model.Explicit,
                DiscNumber = model.DiscNumber,
                TrackNumber = model.TrackNumber,
                AvailableMarkets = StringListConverter.ListToString(model.AvailableMarkets),
                IsPlayable = model.IsPlayable ?? true,
                IsLocal = model.IsLocal,
                PreviewUrl = model.PreviewUrl,
                UpdatedAt = DateTime.UtcNow
            };

            if (newEntity)
                entity.CreatedAt = DateTime.UtcNow;

            return entity;
        }

        public static SpSavedAlbum MapToEntity(this SpotifySavedAlbumItem model, string userId, bool newEntity = false)
        {
            var entity = new SpSavedAlbum
            {
                UserId = userId,
                AlbumId = model.Album.Id,
                AddedAt = DateTime.Parse(model.AddedAt),
                CreatedAt = DateTime.UtcNow
            };

            return entity;
        }

        public static SpTrackArtist MapToTrackArtistEntity(this SpotifyArtistObject model, string trackId, bool newEntity = false)
        {
            var entity = new SpTrackArtist
            {
                TrackId = trackId,
                ArtistId = model.Id,
                CreatedAt = DateTime.UtcNow
            };

            return entity;
        }

        public static SpAlbumArtistAssign MapToAlbumArtistAssignEntity(this SpotifyArtistObject model, string albumId, string albumName, bool newEntity = false)
        {
            var entity = new SpAlbumArtistAssign
            {
                AlbumId = albumId,
                ArtistId = model.Id,
                CreatedAt = DateTime.UtcNow,
                ArtistName = model.Name,
                AlbumName = albumName
            };

            return entity;
        }

        public static LibAlbumUserAssign MapToAlbumUserAssignEntity(this SpotifyAlbumObject model, string userId, bool newEntity = false)
        {
            var entity = new LibAlbumUserAssign
            {
                UserId = userId,
                AlbumId = model.Id,
                AlbumName = model.Name,
                DateCreatedUtc = DateTime.UtcNow
            };

            return entity;
        }

        public static LibArtistUserAssign MapToArtistUserAssignEntity(this SpotifyArtistObject model, string userId, bool newEntity = false)
        {
            var entity = new LibArtistUserAssign
            {
                UserId = userId,
                ArtistId = model.Id,
                ArtistName = model.Name,
                DateCreatedUtc = DateTime.UtcNow
            };

            return entity;
        }

        public static LibTrackUserAssign MapToTrackUserAssignEntity(this SpotifyTrackObject model, string userId, bool newEntity = false)
        {
            var entity = new LibTrackUserAssign
            {
                UserId = userId,
                TrackId = model.Id,
                TrackName = model.Name,
                DateCreatedUtc = DateTime.UtcNow
            };

            return entity;
        }

        public static SpUser MapToEntity(this SpotifyUserProfile model, bool newEntity = false)
        {
            var entity = new SpUser
            {
                Id = model.Id,
                DisplayName = model.DisplayName,
                Type = model.Type,
                Uri = model.Uri,
                Href = model.Href,
                Country = model.Country,
                Email = model.Email,
                Product = model.Product,
                Followers = model.Followers.Total,
                Images = model.Images.ToJsonCompact(),
                UpdatedAt = DateTime.UtcNow
            };

            if (newEntity)
                entity.CreatedAt = DateTime.UtcNow;

            return entity;
        }
    }
}