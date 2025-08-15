using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [Route("api/spotify")]
    [ApiController]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyApiService _spotifyApiService;

        public SpotifyController(SpotifyApiService spotifyApiService)
        {
            _spotifyApiService = spotifyApiService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<object>> GetCurrentUserProfile()
        {
            try
            {
                var profile = await _spotifyApiService.GetCurrentUserProfileAsync();

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

        [HttpGet("playlists")]
        public async Task<ActionResult<object>> GetUserPlaylists()
        {
            try
            {
                var playlists = await _spotifyApiService.GetUserPlaylistsAsync();

                if (playlists == null)
                {
                    return NotFound("Nie udało się pobrać playlist użytkownika");
                }

                return Ok(playlists);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<object>> SearchTracks([FromQuery] string q, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrEmpty(q))
            {
                return BadRequest("Parametr 'q' jest wymagany");
            }

            try
            {
                var tracks = await _spotifyApiService.SearchTracksAsync(q, limit);

                if (tracks == null)
                {
                    return NotFound("Nie udało się wyszukać utworów");
                }

                return Ok(tracks);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("artists/followed")]
        public async Task<ActionResult<object>> GetFollowedArtists()
        {
            try
            {
                var artists = await _spotifyApiService.GetFollowedArtists();

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
                var tracks = await _spotifyApiService.GetUserSavedTracks(limit);

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
                var albums = await _spotifyApiService.GetUserSavedAlbums(limit);

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
                var items = await _spotifyApiService.GetCurrentUserTopItems(type, limit);

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