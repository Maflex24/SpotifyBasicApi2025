using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [Route("api/spotify/my")]
    [ApiController]
    public class SpotifyMyLibraryController : ControllerBase
    {
        private readonly SpotifyMyProfileService service;

        public SpotifyMyLibraryController(SpotifyMyProfileService service)
        {
            this.service = service;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<object>> GetCurrentUserProfile()
        {
            try
            {
                var profile = await service.GetCurrentUserProfileAsync();

                if (profile == null)
                {
                    return NotFound("Nie udało się pobrać profilu użytkownika");
                }

                return Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("artists/followed")]
        public async Task<ActionResult<object>> GetFollowedArtists([FromQuery] int limit = 10)
        {
            try
            {
                var artists = await service.GetFollowedArtists();

                if (artists == null)
                {
                    return NotFound("Nie udało się pobrać wykonawców użytkownika");
                }

                return Ok(artists);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("tracks/saved")]
        public async Task<ActionResult<object>> GetUserSavedTracks([FromQuery] int limit = 10)
        {
            try
            {
                var tracks = await service.GetUserSavedTracks(limit);

                if (tracks == null)
                {
                    return NotFound("Nie udało się pobrać zapisanych utworów");
                }

                return Ok(tracks);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("albums/saved")]
        public async Task<ActionResult<object>> GetUserSavedAlbums([FromQuery] int limit = 10)
        {
            try
            {
                var albums = await service.GetUserSavedAlbums(limit);

                if (albums == null)
                {
                    return NotFound("Nie udało się pobrać zapisanych albumów");
                }

                return Ok(albums);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("top-items")]
        public async Task<ActionResult<object>> GetCurrentUserTopItems([FromQuery] string type = "tracks", [FromQuery] int limit = 10)
        {
            try
            {
                var items = await service.GetCurrentUserTopItems(type, limit);

                if (items == null)
                {
                    return NotFound("Nie udało się pobrać top elementów użytkownika");
                }

                return Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}