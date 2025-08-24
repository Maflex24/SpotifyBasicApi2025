using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/artists")]
    public class ArtistsController : ControllerBase
    {
        private readonly SpotifyApiService spotifyApi;

        public ArtistsController(SpotifyApiService spotifyApi)
        {
            this.spotifyApi = spotifyApi;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var artist = await spotifyApi.GetArtist(id);
            if (artist == null) return NotFound();
            return Ok(artist);
        }

        [HttpGet("{id}/top-tracks")] 
        public async Task<IActionResult> TopTracks(string id, [FromQuery] string market = "PL")
        {
            var tracks = await spotifyApi.GetArtistTopTracksAsync(id, market);
            return Ok(tracks);
        }

        [HttpGet("{id}/albums")] 
        public async Task<IActionResult> Albums(string id, [FromQuery] string includeGroups = "album,single", [FromQuery] string market = "PL")
        {
            var albums = await spotifyApi.GetArtistAlbumsAsync(id, includeGroups, market);
            return Ok(albums);
        }
    }
}


