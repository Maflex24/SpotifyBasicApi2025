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
        public async Task<IActionResult> GetUserArtists(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null)
        {
            try
            {
                var result = await libraryService.GetUserArtistsAsync(sort, order, tags, sortTagId, genre, includeTags, includeMode, excludeTags, excludeMode);

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

        [HttpGet("artists/genres")]
        public async Task<IActionResult> GetArtistsGenres()
        {
            try
            {
                var result = await libraryService.GetArtistsLibraryGenres();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tracks")]
        public async Task<IActionResult> GetUserTracks(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? albumId = null,
            [FromQuery] int? year = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null)
        {
            try
            {
                var result = await libraryService.GetUserTracksAsync(sort, order, tags, sortTagId, artistId, albumId, year, includeTags, includeMode, excludeTags, excludeMode);
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

        [HttpGet("albums")] 
        public async Task<IActionResult> GetUserAlbums(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null)
        {
            try
            {
                var result = await libraryService.GetUserAlbumsAsync(sort, order, tags, sortTagId, artistId, includeTags, includeMode, excludeTags, excludeMode);

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

        [HttpGet("albums/tags")]
        public async Task<IActionResult> GetUserAlbumTags()
        {
            try
            {
                var result = await libraryService.GetUserAlbumTagsAsync();
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

        [HttpGet("albums/artists")]
        public async Task<IActionResult> GetAlbumsArtists()
        {
            try
            {
                var result = await libraryService.GetAlbumsArtistsForUserAsync();
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


