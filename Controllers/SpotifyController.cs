using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SpotifyBasicApi2025.Controllers
{
    [Route("api/spotify")]
    [ApiController]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyApiService _spotifyApiService;
        private readonly SpotifyDataService spotifyDataService;

        public SpotifyController(SpotifyApiService spotifyApiService, SpotifyDataService spotifyDataService)
        {
            _spotifyApiService = spotifyApiService;
            this.spotifyDataService = spotifyDataService;
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
        public async Task<ActionResult<object>> GetFollowedArtists([FromQuery] int limit = 50)
        {
            try
            {
                var artists = await _spotifyApiService.GetFollowedArtists(limit);

                if (artists == null)
                {
                    return NotFound("Nie udało się pobrać śledzonych artystów");
                }

                return Ok(artists);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("artists/followed/all")]
        public async Task<ActionResult<object>> GetAllFollowedArtists()
        {
            try
            {
                var artists = await _spotifyApiService.GetAllFollowedArtists();

                if (artists == null)
                {
                    return NotFound("Nie udało się pobrać wszystkich śledzonych artystów");
                }

                return Ok(new
                {
                    total = artists.Count,
                    artists = artists
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("artists/sync")]
        public async Task<ActionResult<object>> SyncFollowedArtistsToDatabase()
        {
            try
            {
                // Pobierz wszystkich śledzonych artystów z Spotify API
                var spotifyArtists = await _spotifyApiService.GetAllFollowedArtists();

                if (spotifyArtists == null || spotifyArtists.Count == 0)
                {
                    return NotFound("Nie znaleziono śledzonych artystów");
                }

                // Zapisz artystów w bazie danych
                var savedCount = await spotifyDataService.SaveFollowedArtistsAsync(spotifyArtists);

                return Ok(new
                {
                    message = $"Zapisano {savedCount} artystów w bazie danych",
                    totalArtists = spotifyArtists.Count,
                    savedCount = savedCount
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}