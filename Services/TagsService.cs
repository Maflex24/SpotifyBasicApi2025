using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;

namespace SpotifyBasicApi2025.Services
{
    public class TagsService
    {
        private readonly DatabaseContext context;
        private readonly UserService userService;

        public TagsService(DatabaseContext context, UserService userService)
        {
            this.context = context;
            this.userService = userService;
        }

        #region TagsCategories

        public async Task<TagGroupModel> CreateTagGroupAsync(TagGroupModel model, string userId)
        {
            var entity = model.MapToEntity();

            entity.UserId = userId;

            context.LibTagGroups.Add(entity);
            await context.SaveChangesAsync();

            return entity.MapToTagGroupModel();
        }

        #endregion TagsCategories

        public async Task<TagModel> CreateTagAsync(TagModel model)
        {
            var entity = model.MapToEntity();

            context.LibTags.Add(entity);
            await context.SaveChangesAsync();

            return entity.MapToTagModel();
        }
    }
}