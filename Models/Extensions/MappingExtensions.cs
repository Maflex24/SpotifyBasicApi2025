using SpotifyBasicApi2025.Entities;

namespace SpotifyBasicApi2025.Models.Extensions
{
    public static class MappingExtensions
    {
        #region TagGroups

        public static LibTagGroup MapToEntity(this TagGroupModel model)
        {
            return new LibTagGroup
            {
                Id = model.Id,
                Name = model.Name,
                ForAlbum = model.ForAlbum,
                ForArtist = model.ForArtist,
                ForPlaylist = model.ForPlaylist,
                ForTrack = model.ForTrack
            };
        }

        public static TagGroupModel MapToTagGroupModel(this LibTagGroup entity)
        {
            return new TagGroupModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ForAlbum = entity.ForAlbum,
                ForArtist = entity.ForArtist,
                ForPlaylist = entity.ForPlaylist,
                ForTrack = entity.ForTrack
            };
        }

        #endregion TagGroups

        public static LibTag MapToEntity(this TagModel model)
        {
            return new LibTag
            {
                Id = model.Id,
                Name = model.Name,
                ForAlbum = model.ForAlbum,
                ForArtist = model.ForArtist,
                ForPlaylist = model.ForPlaylist,
                ForTrack = model.ForTrack,
                TagGroupId = model.TagGroupId
            };
        }

        public static TagModel MapToTagModel(this LibTag entity)
        {
            return new TagModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ForAlbum = entity.ForAlbum,
                ForArtist = entity.ForArtist,
                ForPlaylist = entity.ForPlaylist,
                ForTrack = entity.ForTrack
            };
        }
    }
}