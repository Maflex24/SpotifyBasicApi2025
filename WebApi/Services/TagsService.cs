using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;
using System.Text.RegularExpressions;

namespace SpotifyBasicApi2025.Services
{
    public class TagsService
    {
        public sealed class TagNameValidationException : Exception
        {
            public TagNameValidationException(string message) : base(message) { }
        }

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

        public async Task<List<TagGroupModel>> GetAllTagGroupsAsync(string userId)
        {
            var entities = await context.LibTagGroups
                .Where(g => g.UserId == userId)
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

        public async Task<TagModel> CreateTagAsync(TagModel model, string userId)
        {
            var names = await ValidateTagNamesAsync(model, userId);
            var entity = model.MapToEntity();
            entity.Name = names.MainName;
            entity.Aliases = names.Aliases
                .Select(alias => new LibTagAlias
                {
                    UserId = userId,
                    Name = alias.Name,
                    NormalizedName = alias.NormalizedName
                })
                .ToList();

            context.LibTags.Add(entity);
            await context.SaveChangesAsync();

            return entity.MapToTagModel();
        }

        public async Task<List<TagModel>> GetAllTagsAsync(string userId)
        {
            var entities = await context.LibTags
                .AsNoTracking()
                .Include(t => t.Aliases)
                .Where(t => t.TagGroup.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return entities.Select(e => e.MapToTagModel()).ToList();
        }

        public async Task<List<TagModel>> GetTagsList(string userId, string categoriesStringIds)
        {
            var categoriesIds = new List<long>();

            if (!string.IsNullOrEmpty(categoriesStringIds))
                categoriesIds = categoriesStringIds.Trim().Split(',').ToList().Select(id => long.Parse(id)).ToList();

            var query = context.LibTags.AsQueryable();

            if (categoriesIds.Any())
                query = query.Where(t => categoriesIds.Contains(t.TagGroupId));

            var entities = await query
                .AsNoTracking()
                .Include(t => t.Aliases)
                .Where(t => t.TagGroup.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var tagIds = entities.Select(tag => tag.Id).ToList();
            var assignmentCounts = await context.LibTagAssigns
                .AsNoTracking()
                .Where(assign => assign.UserId == userId && tagIds.Contains(assign.TagId))
                .GroupBy(assign => assign.TagId)
                .Select(group => new { TagId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.TagId, item => item.Count);

            return entities.Select(entity =>
            {
                var model = entity.MapToTagModel();
                model.AssignmentCount = assignmentCounts.GetValueOrDefault(entity.Id);
                return model;
            }).ToList();
        }

        public async Task<TagModel?> GetTagByIdAsync(int id)
        {
            var entity = await context.LibTags
                .Include(t => t.Aliases)
                .FirstOrDefaultAsync(t => t.Id == id);

            return entity?.MapToTagModel();
        }

        public async Task<bool> SetTagPinnedAsync(long id, bool isPinned, string userId)
        {
            var entity = await context.LibTags
                .Include(tag => tag.TagGroup)
                .FirstOrDefaultAsync(tag => tag.Id == id && tag.TagGroup.UserId == userId);

            if (entity is null)
                return false;

            entity.IsPinned = isPinned;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserOwnsTagGroupAsync(long tagGroupId, string userId)
        {
            return await context.LibTagGroups
                .AnyAsync(group => group.Id == tagGroupId && group.UserId == userId);
        }

        public async Task<TagModel?> UpdateTagAsync(int id, TagModel model, string userId)
        {
            var entity = await context.LibTags
                .Include(t => t.TagGroup)
                .Include(t => t.Aliases)
                .FirstOrDefaultAsync(t => t.Id == id && t.TagGroup.UserId == userId);

            if (entity == null)
                return null;

            var names = await ValidateTagNamesAsync(model, userId, entity.Id);

            entity.Name = names.MainName;
            entity.TagGroupId = model.TagGroupId;
            entity.ForAlbum = model.ForAlbum;
            entity.ForArtist = model.ForArtist;
            entity.ForTrack = model.ForTrack;
            entity.ForPlaylist = model.ForPlaylist;

            var aliasesByNormalizedName = entity.Aliases
                .ToDictionary(alias => alias.NormalizedName, StringComparer.Ordinal);
            var requestedAliases = names.Aliases
                .ToDictionary(alias => alias.NormalizedName, StringComparer.Ordinal);

            foreach (var alias in entity.Aliases.Where(alias => !requestedAliases.ContainsKey(alias.NormalizedName)).ToList())
            {
                context.LibTagAliases.Remove(alias);
                entity.Aliases.Remove(alias);
            }

            foreach (var requestedAlias in names.Aliases)
            {
                if (aliasesByNormalizedName.TryGetValue(requestedAlias.NormalizedName, out var existingAlias))
                {
                    existingAlias.Name = requestedAlias.Name;
                    continue;
                }

                entity.Aliases.Add(new LibTagAlias
                {
                    UserId = userId,
                    Name = requestedAlias.Name,
                    NormalizedName = requestedAlias.NormalizedName
                });
            }

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

        private async Task<TagNameSet> ValidateTagNamesAsync(TagModel model, string userId, long? editedTagId = null)
        {
            var mainName = CanonicalizeName(model.Name);
            if (mainName is null)
                throw new TagNameValidationException("Nazwa tagu jest wymagana i może mieć maksymalnie 255 znaków.");

            var aliases = (model.Aliases ?? new List<string>())
                .Select(CanonicalizeName)
                .ToList();

            if (aliases.Any(alias => alias is null))
                throw new TagNameValidationException("Każda nazwa alternatywna jest wymagana i może mieć maksymalnie 255 znaków.");

            var requestedNames = new[] { new TagName(mainName, NormalizeName(mainName)) }
                .Concat(aliases.Select(alias => new TagName(alias!, NormalizeName(alias!))))
                .ToList();

            if (requestedNames.Select(name => name.NormalizedName).Distinct(StringComparer.Ordinal).Count() != requestedNames.Count)
                throw new TagNameValidationException("Nazwa główna i nazwy alternatywne muszą być różne.");

            var otherTagNames = await context.LibTags
                .AsNoTracking()
                .Where(tag => tag.TagGroup.UserId == userId && (!editedTagId.HasValue || tag.Id != editedTagId.Value))
                .Select(tag => tag.Name)
                .ToListAsync();

            var otherAliasNames = await context.LibTagAliases
                .AsNoTracking()
                .Where(alias => alias.UserId == userId && (!editedTagId.HasValue || alias.TagId != editedTagId.Value))
                .Select(alias => alias.NormalizedName)
                .ToListAsync();

            var occupiedNames = otherTagNames
                .Select(NormalizeName)
                .Concat(otherAliasNames)
                .ToHashSet(StringComparer.Ordinal);

            if (requestedNames.Any(name => occupiedNames.Contains(name.NormalizedName)))
                throw new TagNameValidationException("Ta nazwa jest już używana przez inny tag lub jego nazwę alternatywną.");

            return new TagNameSet(mainName, requestedNames.Skip(1).ToList());
        }

        private static string? CanonicalizeName(string? name)
        {
            var canonical = Regex.Replace(name?.Trim() ?? string.Empty, @"\s+", " ");
            return canonical.Length is > 0 and <= 255 ? canonical : null;
        }

        private static string NormalizeName(string name) => CanonicalizeName(name)!.ToUpperInvariant();

        private sealed record TagName(string Name, string NormalizedName);
        private sealed record TagNameSet(string MainName, List<TagName> Aliases);

        #endregion Tags
    }
}
