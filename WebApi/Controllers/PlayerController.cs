using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/player")]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerService playerService;

        public PlayerController(PlayerService playerService)
        {
            this.playerService = playerService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetState([FromQuery] string? market = null)
        {
            var result = await playerService.GetPlaybackStateAsync(market);
            return Ok(result);
        }

        [HttpPut("play")]
        public async Task<IActionResult> Play([FromQuery] string? deviceId = null, [FromBody] object? body = null)
        {
            await playerService.PlayAsync(deviceId, body);
            return NoContent();
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
    }
}


