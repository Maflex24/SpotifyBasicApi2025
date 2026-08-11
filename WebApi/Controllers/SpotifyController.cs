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

        public SpotifyController(SpotifyApiService spotifyApiService)
        {
            _spotifyApiService = spotifyApiService;
        }

        #region Artists

        [HttpGet("artists/{artistId}")]
        public async Task<ActionResult<object>> GetFollowedArtists(
            [FromRoute] string artistId)
        {
            try
            {
                var artist = await _spotifyApiService.GetAndSaveArtist(artistId);

                if (artist == null)
                {
                    return NotFound("Nie udało się pobrać śledzonych artystów");
                }

                return Ok(artist);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("artists/{artistId}/top-tracks")]
        public async Task<ActionResult<object>> GetArtistTopTracks([FromRoute] string artistId, [FromQuery] string market = "PL")
        {
            try
            {
                var tracks = await _spotifyApiService.GetArtistTopTracksAsync(artistId, market);

                return Ok(new { items = tracks });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("artists/{artistId}/related")]
        public async Task<ActionResult<object>> GetRelatedArtists([FromRoute] string artistId)
        {
            return Ok(await _spotifyApiService.GetRelatedArtistsAsync(artistId));
        }

        [HttpGet("artists/catalog-search")]
        public async Task<ActionResult<object>> SearchArtistsCatalog([FromQuery] string q, [FromQuery] int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Parametr 'q' jest wymagany.");

            var artists = await _spotifyApiService.SearchArtistsAsync(q, limit);
            return Ok(artists);
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
                var syncResults = await _spotifyApiService.SyncUserArtists();

                return Ok(syncResults);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion Artists

        #region Albums

        [HttpPost("albums/sync")]
        public async Task<ActionResult<object>> SyncUserSavedAlbums()
        {
            try
            {
                var syncResults = await _spotifyApiService.SyncUserSavedAlbums();

                return Ok(syncResults);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("artists/{artistId}/add-albums")]
        public async Task<ActionResult<object>> AddAllArtistAlbumsToLibrary([FromRoute] string artistId)
        {
            try
            {
                var result = await _spotifyApiService.AddArtistAlbumsToUserLibraryAsync(artistId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion Albums

        #region Tracks

        [HttpPost("tracks/sync")]
        public async Task<ActionResult<object>> SyncUserSavedTracks()
        {
            try
            {
                var syncResults = await _spotifyApiService.SyncUserSavedTracks();

                return Ok(syncResults);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion Tracks

        #region Playlists

        [HttpGet("playlists")]
        public async Task<ActionResult<object>> GetUserPlaylists()
        {
            try
            {
                var playlists = await _spotifyApiService.GetUserPlaylists();

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

        [HttpPost("playlists/sync")]
        public async Task<ActionResult<object>> SyncUserPlaylists()
        {
            try
            {
                var syncResults = await _spotifyApiService.SyncUserPlaylists();

                return Ok(syncResults);
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

        [HttpPost("sync")]
        public async Task<ActionResult<object>> SyncSpotifyLibrary()
        {
            try
            {
                // Pobierz wszystkich śledzonych artystów z Spotify API
                var artistSyncResults = await _spotifyApiService.SyncUserArtists();
                var albumsSyncResults = await _spotifyApiService.SyncUserSavedAlbums();
                var stacksSyncResults = await _spotifyApiService.SyncUserSavedTracks();
                var playlistsSyncResults = await _spotifyApiService.SyncUserPlaylists();

                return Ok(new
                {
                    artistSyncResults = artistSyncResults,
                    albumsSyncResults = albumsSyncResults,
                    stacksSyncResults = stacksSyncResults,
                    playlistsSyncResults = playlistsSyncResults
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
