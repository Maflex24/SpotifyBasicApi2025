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
    }
}