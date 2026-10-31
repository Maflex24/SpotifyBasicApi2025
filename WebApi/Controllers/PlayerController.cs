using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/player")]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerService playerService;
        private readonly SpotifyApiService spotifyApiService;

        public PlayerController(PlayerService playerService, SpotifyApiService spotifyApiService)
        {
            this.playerService = playerService;
            this.spotifyApiService = spotifyApiService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetState([FromQuery] string? market = null)
        {
            try
            {
                var result = await playerService.GetPlaybackStateAsync(market);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices()
        {
            try
            {
                var result = await playerService.GetDevicesAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPut("play")]
        public async Task<IActionResult> Play([FromQuery] string? deviceId = null, [FromBody] object? body = null)
        {
            try
            {
                var result = await playerService.PlayAsync(deviceId, body);
                if (result.Warning != null)
                    return Ok(new { warning = result.Warning });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, ex.Message);
            }
        }

        [HttpPut("pause")]
        public async Task<IActionResult> Pause([FromQuery] string? deviceId = null)
        {
            await playerService.PauseAsync(deviceId);
            return NoContent();
        }

        [HttpPost("next")]
        public async Task<IActionResult> Next([FromQuery] string? deviceId = null)
        {
            await playerService.NextAsync(deviceId);
            return NoContent();
        }

        [HttpPost("queue/albums/{albumId}")]
        public async Task<IActionResult> QueueAlbum(string albumId)
        {
            try
            {
                var tracks = await spotifyApiService.GetAllAlbumTracks(albumId);
                var result = await playerService.QueueAlbumAsync(albumId, tracks.Select(track => track.Uri));
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(502, ex.Message);
            }
        }

        [HttpPost("queue/albums")]
        public async Task<IActionResult> QueueAlbums([FromBody] QueueAlbumsRequest request)
        {
            var albumIds = request.AlbumIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (albumIds.Count == 0)
                return Ok(new { albumsProcessed = 0, failedAlbumIds = Array.Empty<string>() });

            var processedAlbums = 0;
            var failedAlbumIds = new List<string>();

            foreach (var albumId in albumIds)
            {
                try
                {
                    var tracks = await spotifyApiService.GetAllAlbumTracks(albumId);
                    await playerService.QueueAlbumAsync(albumId, tracks.Select(track => track.Uri));
                    processedAlbums++;
                }
                catch (InvalidOperationException)
                {
                    failedAlbumIds.Add(albumId);
                }
            }

            if (processedAlbums == 0)
                return StatusCode(502, new { message = "Nie udało się dodać żadnego albumu do kolejki.", failedAlbumIds });

            return Ok(new { albumsProcessed = processedAlbums, failedAlbumIds });
        }

        [HttpPost("previous")]
        public async Task<IActionResult> Previous([FromQuery] string? deviceId = null)
        {
            await playerService.PreviousAsync(deviceId);
            return NoContent();
        }

        [HttpPut("seek")]
        public async Task<IActionResult> Seek([FromQuery] int positionMs, [FromQuery] string? deviceId = null)
        {
            await playerService.SeekAsync(positionMs, deviceId);
            return NoContent();
        }

        [HttpPut("volume")]
        public async Task<IActionResult> Volume([FromQuery] int volumePercent, [FromQuery] string? deviceId = null)
        {
            await playerService.SetVolumeAsync(volumePercent, deviceId);
            return NoContent();
        }

        [HttpPut]
        public async Task<IActionResult> Transfer([FromQuery] string deviceIdsCsv, [FromQuery] bool? play = null)
        {
            var deviceIds = deviceIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            await playerService.TransferAsync(deviceIds, play);
            return NoContent();
        }
    }

    public sealed class QueueAlbumsRequest
    {
        public List<string> AlbumIds { get; set; } = new();
    }
}


