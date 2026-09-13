using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Enums;
using SpotifyBasicApi2025.Models.Pagination;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly UserService userService;
        private readonly DatabaseContext dbContext;
        private readonly TagAssignmentHistoryService assignmentHistoryService;

        public TagsController(TagsService tagsService, SpotifyApiService spotifyApiService, UserService userService, DatabaseContext dbContext, TagAssignmentHistoryService assignmentHistoryService)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.userService = userService;
            this.dbContext = dbContext;
            this.assignmentHistoryService = assignmentHistoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            try
            {
                var tags = await tagsService.GetAllTagsAsync(userId);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagów: {ex.Message}");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetTagsList(
            [FromQuery] string categoriesIds)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            try
            {
                var tags = await tagsService.GetTagsList(userId, categoriesIds);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagów: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagById(int id)
        {
            try
            {
                var tag = await tagsService.GetTagByIdAsync(id);

                if (tag == null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagu: {ex.Message}");
            }
        }

        public sealed class PinTagRequest
        {
            public bool IsPinned { get; set; }
        }

        [HttpPut("{id}/pin")]
        public async Task<IActionResult> SetTagPinned(long id, [FromBody] PinTagRequest request)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Użytkownik nie jest zalogowany");

            var updated = await tagsService.SetTagPinnedAsync(id, request.IsPinned, userId);
            return updated ? NoContent() : NotFound($"Tag o ID {id} nie został znaleziony");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = await userService.GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                if (!await tagsService.UserOwnsTagGroupAsync(tagModel.TagGroupId, userId))
                    return BadRequest("Wybrana grupa tagów nie istnieje lub nie należy do użytkownika");

                var createdTag = await tagsService.CreateTagAsync(tagModel, userId);

                return CreatedAtAction(nameof(GetTagById), new { id = createdTag.Id }, createdTag);
            }
            catch (TagsService.TagNameValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas tworzenia tagu: {ex.Message}");
            }
        }

        // --- Assigns ---

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTag([FromBody] TagAssignModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            if (model.TagId <= 0 || string.IsNullOrWhiteSpace(model.ItemType) || string.IsNullOrWhiteSpace(model.SpotifyItemId))
                return BadRequest("Nieprawidłowe dane przypisania tagu");

            var existing = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            var strength = Math.Clamp(model.Strength, 0.0f, 6.0f);

            if (existing is not null)
            {
                if (existing.Strength == strength)
                    return NoContent();

                await assignmentHistoryService.RecordAsync(userId, existing, TagAssignmentHistoryAction.StrengthChanged, existing.Strength, strength);
                existing.Strength = strength;
                existing.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return NoContent();
            }

            var entity = new LibTagAssign
            {
                TagId = model.TagId,
                UserId = userId,
                SpotifyItemId = model.SpotifyItemId,
                ItemType = model.ItemType,
                Strength = strength,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.LibTagAssigns.Add(entity);
            await assignmentHistoryService.RecordAsync(userId, entity, TagAssignmentHistoryAction.Added, null, strength);
            await dbContext.SaveChangesAsync();

            return Created($"api/tags/assign/{entity.Id}", entity.Id);
        }

        [HttpPut("assign")]
        public async Task<IActionResult> UpdateAssign([FromBody] TagAssignModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            var entity = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            var strength = Math.Clamp(model.Strength, 0.0f, 6.0f);

            if (entity is null)
            {
                // Upsert: utwórz jeśli nie istnieje
                var created = new LibTagAssign
                {
                    TagId = model.TagId,
                    UserId = userId,
                    SpotifyItemId = model.SpotifyItemId,
                    ItemType = model.ItemType,
                    Strength = strength,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.LibTagAssigns.Add(created);
                await assignmentHistoryService.RecordAsync(userId, created, TagAssignmentHistoryAction.Added, null, strength);
                await dbContext.SaveChangesAsync();
                return NoContent();
            }

            if (entity.Strength == strength)
                return NoContent();

            await assignmentHistoryService.RecordAsync(userId, entity, TagAssignmentHistoryAction.StrengthChanged, entity.Strength, strength);
            entity.Strength = strength;
            entity.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("assign/bulk")]
        public async Task<IActionResult> MergeAssigns([FromBody] BulkTagAssignModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (userId is null)
                return StatusCode(403);

            var itemType = model.ItemType?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(model.SpotifyItemId)
                || itemType is not ("artist" or "album" or "track" or "playlist")
                || model.Tags is null
                || model.Tags.Count == 0
                || model.Tags.Any(tag => tag.TagId <= 0 || float.IsNaN(tag.Strength) || float.IsInfinity(tag.Strength))
                || model.Tags.Select(tag => tag.TagId).Distinct().Count() != model.Tags.Count)
                return BadRequest("Nieprawidłowe dane przypisań tagów.");

            var tagIds = model.Tags.Select(tag => tag.TagId).ToList();
            var ownedTagIds = await dbContext.LibTags
                .Where(tag => tagIds.Contains(tag.Id) && tag.TagGroup.UserId == userId)
                .Select(tag => tag.Id)
                .ToListAsync();

            if (ownedTagIds.Count != tagIds.Count)
                return BadRequest("Co najmniej jeden tag nie istnieje lub nie należy do użytkownika.");

            var existing = await dbContext.LibTagAssigns
                .Where(assign => assign.UserId == userId
                    && assign.ItemType == itemType
                    && assign.SpotifyItemId == model.SpotifyItemId
                    && tagIds.Contains(assign.TagId))
                .ToDictionaryAsync(assign => assign.TagId);

            var now = DateTime.UtcNow;
            foreach (var tag in model.Tags)
            {
                var strength = Math.Clamp(tag.Strength, 0.0f, 6.0f);
                if (existing.TryGetValue(tag.TagId, out var assign))
                {
                    if (assign.Strength == strength)
                        continue;

                    await assignmentHistoryService.RecordAsync(userId, assign, TagAssignmentHistoryAction.StrengthChanged, assign.Strength, strength, now);
                    assign.Strength = strength;
                    assign.UpdatedAt = now;
                    continue;
                }

                var created = new LibTagAssign
                {
                    TagId = tag.TagId,
                    UserId = userId,
                    SpotifyItemId = model.SpotifyItemId,
                    ItemType = itemType,
                    Strength = strength,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                dbContext.LibTagAssigns.Add(created);
                await assignmentHistoryService.RecordAsync(userId, created, TagAssignmentHistoryAction.Added, null, strength, now);
            }

            await dbContext.SaveChangesAsync();

            var result = await dbContext.LibTagAssigns
                .AsNoTracking()
                .Where(assign => assign.UserId == userId && assign.ItemType == itemType && assign.SpotifyItemId == model.SpotifyItemId)
                .OrderBy(assign => assign.Tag.Name)
                .Select(assign => new TagAssignmentStateModel
                {
                    TagId = assign.TagId,
                    TagName = assign.Tag.Name,
                    TagGroupName = assign.Tag.TagGroup.Name,
                    Strength = assign.Strength
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpDelete("assign")]
        public async Task<IActionResult> RemoveAssign([FromBody] TagAssignModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            var entity = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            if (entity is null)
                return NotFound();

            await assignmentHistoryService.RecordAsync(userId, entity, TagAssignmentHistoryAction.Removed, entity.Strength, null);
            dbContext.LibTagAssigns.Remove(entity);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("assign/{itemType}/{spotifyItemId}")]
        public async Task<IActionResult> GetAssignsForItem([FromRoute] string itemType, [FromRoute] string spotifyItemId)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (userId is null)
                return StatusCode(403);

            var normalizedItemType = itemType.Trim().ToLowerInvariant();
            if (normalizedItemType is not ("artist" or "album" or "track" or "playlist") || string.IsNullOrWhiteSpace(spotifyItemId))
                return BadRequest("Nieprawidłowy typ lub identyfikator elementu");

            var assigns = await dbContext.LibTagAssigns
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.ItemType == normalizedItemType && a.SpotifyItemId == spotifyItemId)
                .OrderBy(a => a.Tag.Name)
                .Select(a => new TagAssignmentStateModel
                {
                    TagId = a.TagId,
                    TagName = a.Tag.Name,
                    TagGroupName = a.Tag.TagGroup.Name,
                    Strength = a.Strength
                })
                .ToListAsync();

            return Ok(assigns);
        }

        [HttpGet("assignment-history")]
        public async Task<IActionResult> GetAssignmentHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? itemType = null,
            [FromQuery] long? tagId = null,
            [FromQuery] string? action = null)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (userId is null)
                return StatusCode(403);

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = dbContext.LibTagAssignmentHistories
                .AsNoTracking()
                .Where(history => history.UserId == userId);

            if (!string.IsNullOrWhiteSpace(itemType))
            {
                itemType = itemType.Trim().ToLowerInvariant();
                if (!SpotifyItemType.IsValid(itemType))
                    return BadRequest("Nieprawidłowy typ elementu.");
                query = query.Where(history => history.ItemType == itemType);
            }

            if (tagId.HasValue)
                query = query.Where(history => history.TagId == tagId.Value);

            if (!string.IsNullOrWhiteSpace(action))
            {
                action = action.Trim();
                if (!TagAssignmentHistoryAction.IsValid(action))
                    return BadRequest("Nieprawidłowy typ akcji.");
                query = query.Where(history => history.Action == action);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(history => history.ChangedAt)
                .ThenByDescending(history => history.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(history => new TagAssignmentHistoryModel
                {
                    Id = history.Id,
                    Action = history.Action,
                    TagId = history.TagId,
                    TagName = history.TagName,
                    TagGroupName = history.TagGroupName,
                    ItemType = history.ItemType,
                    SpotifyItemId = history.SpotifyItemId,
                    ItemName = history.ItemName,
                    PreviousStrength = history.PreviousStrength,
                    NewStrength = history.NewStrength,
                    ChangedAt = history.ChangedAt
                })
                .ToListAsync();

            return Ok(new PagedResult<TagAssignmentHistoryModel>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = await userService.GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                if (!await tagsService.UserOwnsTagGroupAsync(tagModel.TagGroupId, userId))
                    return BadRequest("Wybrana grupa tagów nie istnieje lub nie należy do użytkownika");

                var updatedTag = await tagsService.UpdateTagAsync(id, tagModel, userId);

                if (updatedTag == null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return Ok(updatedTag);
            }
            catch (TagsService.TagNameValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas aktualizacji tagu: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var userId = await userService.GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                var tag = await dbContext.LibTags
                    .Include(item => item.TagGroup)
                    .FirstOrDefaultAsync(item => item.Id == id && item.TagGroup.UserId == userId);
                if (tag is null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                var assignments = await dbContext.LibTagAssigns
                    .Where(item => item.UserId == userId && item.TagId == tag.Id)
                    .ToListAsync();
                foreach (var assignment in assignments)
                    await assignmentHistoryService.RecordAsync(userId, assignment, TagAssignmentHistoryAction.Removed, assignment.Strength, null);

                dbContext.LibTags.Remove(tag);
                await dbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania tagu: {ex.Message}");
            }
        }
    }
}
