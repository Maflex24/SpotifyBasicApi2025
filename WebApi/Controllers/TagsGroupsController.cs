using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/tags-group")]
    public class TagsGroupsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly UserService userService;

        public TagsGroupsController(TagsService tagsService, SpotifyApiService spotifyApiService, UserService userService)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTagGroups()
        {
            try
            {
                var tagGroups = await tagsService.GetAllTagGroupsAsync();

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
                var success = await tagsService.DeleteTagGroupAsync(id);

                if (!success)
                    return NotFound($"Grupa tagów o ID {id} nie została znaleziona");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania grupy tagów: {ex.Message}");
            }
        }
    }
}