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

        public async Task<List<TagGroupModel>> GetAllTagGroupsAsync()
        {
            var entities = await context.LibTagGroups
                .OrderBy(tg => tg.Name)
                .ToListAsync();

            return entities.Select(e => e.MapToTagGroupModel()).ToList();
        }

        public async Task<TagGroupModel?> GetTagGroupByIdAsync(int id)
        {
            var entity = await context.LibTagGroups
                .FirstOrDefaultAsync(tg => tg.Id == id);

            return entity?.MapToTagGroupModel();
        }

        public async Task<TagGroupModel?> UpdateTagGroupAsync(int id, TagGroupModel model)
        {
            var entity = await context.LibTagGroups
                .FirstOrDefaultAsync(tg => tg.Id == id);

            if (entity == null)
                return null;

            entity.Name = model.Name;

            await context.SaveChangesAsync();

            return entity.MapToTagGroupModel();
        }

        public async Task<bool> DeleteTagGroupAsync(int id)
        {
            var entity = await context.LibTagGroups
                .FirstOrDefaultAsync(tg => tg.Id == id);

            if (entity == null)
                return false;

            context.LibTagGroups.Remove(entity);
            await context.SaveChangesAsync();

            return true;
        }

        #endregion TagsCategories

        #region Tags

        public async Task<TagModel> CreateTagAsync(TagModel model)
        {
            var entity = model.MapToEntity();

            context.LibTags.Add(entity);
            await context.SaveChangesAsync();

            return entity.MapToTagModel();
        }

        public async Task<List<TagModel>> GetAllTagsAsync()
        {
            var entities = await context.LibTags
                .OrderBy(t => t.Name)
                .ToListAsync();

            return entities.Select(e => e.MapToTagModel()).ToList();
        }

        public async Task<TagModel?> GetTagByIdAsync(int id)
        {
            var entity = await context.LibTags
                .FirstOrDefaultAsync(t => t.Id == id);

            return entity?.MapToTagModel();
        }

        public async Task<TagModel?> UpdateTagAsync(int id, TagModel model)
        {
            var entity = await context.LibTags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null)
                return null;

            entity.Name = model.Name;

            await context.SaveChangesAsync();

            return entity.MapToTagModel();
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var entity = await context.LibTags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null)
                return false;

            context.LibTags.Remove(entity);
            await context.SaveChangesAsync();

            return true;
        }

        #endregion Tags
    }
}