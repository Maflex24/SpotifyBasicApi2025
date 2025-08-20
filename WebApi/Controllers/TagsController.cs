using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
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

        public TagsController(TagsService tagsService, SpotifyApiService spotifyApiService, UserService userService, DatabaseContext dbContext)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.userService = userService;
            this.dbContext = dbContext;
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

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdTag = await tagsService.CreateTagAsync(tagModel);

                return CreatedAtAction(nameof(GetTagById), new { id = createdTag.Id }, createdTag);
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
                await dbContext.SaveChangesAsync();
                return NoContent();
            }

            entity.Strength = strength;
            entity.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return NoContent();
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

            var assigns = await dbContext.LibTagAssigns
                .Where(a => a.UserId == userId && a.ItemType == itemType && a.SpotifyItemId == spotifyItemId)
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => new { a.TagId, a.Strength })
                .ToListAsync();

            return Ok(assigns);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedTag = await tagsService.UpdateTagAsync(id, tagModel);

                if (updatedTag == null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return Ok(updatedTag);
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
                var success = await tagsService.DeleteTagAsync(id);

                if (!success)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania tagu: {ex.Message}");
            }
        }
    }
}