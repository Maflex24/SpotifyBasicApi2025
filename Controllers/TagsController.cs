using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly UserService userService;

        public TagsController(TagsService tagsService, SpotifyApiService spotifyApiService, UserService userService)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            try
            {
                var tags = await tagsService.GetAllTagsAsync();
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