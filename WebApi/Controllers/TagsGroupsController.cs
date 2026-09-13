using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/tags-group")]
    public class TagsGroupsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly UserService userService;
        private readonly DatabaseContext dbContext;
        private readonly TagAssignmentHistoryService assignmentHistoryService;

        public TagsGroupsController(TagsService tagsService, SpotifyApiService spotifyApiService, UserService userService, DatabaseContext dbContext, TagAssignmentHistoryService assignmentHistoryService)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.userService = userService;
            this.dbContext = dbContext;
            this.assignmentHistoryService = assignmentHistoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTagGroups()
        {
            try
            {
                var userId = await userService.GetCurrentUserIdAsync();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                var tagGroups = await tagsService.GetAllTagGroupsAsync(userId);

                return Ok(tagGroups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania grup tagów: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagGroupById(int id)
        {
            try
            {
                var tagGroup = await tagsService.GetTagGroupByIdAsync(id);

                if (tagGroup == null)
                    return NotFound($"Grupa tagów o ID {id} nie została znaleziona");

                return Ok(tagGroup);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania grupy tagów: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTagGroup([FromBody] TagGroupModel tagGroupModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = await userService.GetCurrentUserIdAsync();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                var createdTagGroup = await tagsService.CreateTagGroupAsync(tagGroupModel, userId);

                return CreatedAtAction(nameof(GetTagGroupById), new { id = createdTagGroup.Id }, createdTagGroup);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas tworzenia grupy tagów: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTagGroup(int id, [FromBody] TagGroupModel tagGroupModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedTagGroup = await tagsService.UpdateTagGroupAsync(id, tagGroupModel);

                if (updatedTagGroup == null)
                    return NotFound($"Grupa tagów o ID {id} nie została znaleziona");

                return Ok(updatedTagGroup);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas aktualizacji grupy tagów: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTagGroup(int id)
        {
            try
            {
                var userId = await userService.GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Użytkownik nie jest zalogowany");

                var group = await dbContext.LibTagGroups
                    .Include(item => item.Tags)
                    .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
                if (group is null)
                    return NotFound($"Grupa tagów o ID {id} nie została znaleziona");

                var tagIds = group.Tags.Select(tag => tag.Id).ToList();
                var assignments = await dbContext.LibTagAssigns
                    .Where(item => item.UserId == userId && tagIds.Contains(item.TagId))
                    .ToListAsync();
                foreach (var assignment in assignments)
                    await assignmentHistoryService.RecordAsync(userId, assignment, TagAssignmentHistoryAction.Removed, assignment.Strength, null);

                dbContext.LibTagGroups.Remove(group);
                await dbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania grupy tagów: {ex.Message}");
            }
        }
    }
}
