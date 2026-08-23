using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers;

[ApiController]
[Route("api/admin/ai")]
public sealed class AiAdminController(AiSettingsService settingsService) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<ActionResult<AiSettingsModel>> GetSettings()
    {
        try
        {
            await settingsService.RequireAdministratorAsync();
            return Ok(await settingsService.GetModelAsync());
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] AiSettingsModel model)
    {
        try
        {
            await settingsService.UpdateAsync(model);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("models")]
    public async Task<ActionResult<List<AiModelOption>>> GetModels()
    {
        try { return Ok(await settingsService.GetAvailableModelsAsync()); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpGet("usage")]
    public async Task<ActionResult<AiUsageSummary>> GetUsage()
    {
        try { return Ok(await settingsService.GetUsageSummaryAsync()); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }
}
