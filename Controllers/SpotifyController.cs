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

        #region Artists

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

                var savedAssing = await spotifyDataService.SaveArtistUserAssign(spotifyArtists);
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

        #endregion Artists

        #region Playlists

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

        #endregion Playlists

        #region Profile

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

        [HttpGet("profile/database")]
        public async Task<ActionResult<object>> GetCurrentUserFromDatabase()
        {
            try
            {
                // Pobierz profil z Spotify API
                var spotifyProfile = await _spotifyApiService.GetCurrentUserProfileAsync();

                if (spotifyProfile == null)
                {
                    return NotFound("Nie udało się pobrać profilu użytkownika z Spotify");
                }

                // Pobierz użytkownika z bazy danych
                var dbUser = await spotifyDataService.GetUserByIdAsync(spotifyProfile.Id);

                if (dbUser == null)
                {
                    return NotFound("Użytkownik nie został znaleziony w bazie danych");
                }

                return Ok(new
                {
                    spotifyProfile = spotifyProfile,
                    databaseUser = dbUser
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion Profile

        #region Search

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

        #endregion Search
    }
}