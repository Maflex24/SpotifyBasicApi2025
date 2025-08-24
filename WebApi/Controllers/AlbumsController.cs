using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/albums")] 
    public class AlbumsController : ControllerBase
    {
        private readonly SpotifyApiService spotifyApi;

        public AlbumsController(SpotifyApiService spotifyApi)
        {
            this.spotifyApi = spotifyApi;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var album = await spotifyApi.GetAlbumAsync(id);
            if (album == null) return NotFound();
            return Ok(album);
        }

        [HttpGet("{id}/tracks")] 
        public async Task<IActionResult> Tracks(string id)
        {
            var tracks = await spotifyApi.GetAllAlbumTracks(id);
            return Ok(new { items = tracks });
        }
    }
}


