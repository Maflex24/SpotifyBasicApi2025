using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/library")]
    public class LibraryController : ControllerBase
    {
        private readonly LibraryService libraryService;

        public LibraryController(LibraryService libraryService)
        {
            this.libraryService = libraryService;
        }

        [HttpGet("artists")] 
        public async Task<IActionResult> GetUserArtists([FromQuery] string? sort = null, [FromQuery] string? order = null, [FromQuery] string? tags = null, [FromQuery] long? sortTagId = null)
        {
            try
            {
                var result = await libraryService.GetUserArtistsAsync(sort, order, tags, sortTagId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("artists/tags")]
        public async Task<IActionResult> GetUserArtistTags()
        {
            try
            {
                var result = await libraryService.GetUserArtistTagsAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}


