using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController(AiChatService chatService, AiSettingsService settingsService) : ControllerBase
{
    [HttpGet("access")]
    public async Task<ActionResult<AiAccessModel>> GetAccess() => Ok(await settingsService.GetCurrentAccessAsync());

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest("Wiadomość nie może być pusta.");
        return Ok(await chatService.ChatAsync(request, cancellationToken));
    }
}
