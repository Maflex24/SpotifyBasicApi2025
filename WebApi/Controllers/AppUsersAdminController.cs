using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers;

[ApiController]
[Route("api/admin/app-users")]
public sealed class AppUsersAdminController(AppUserAuthService appUserAuthService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppUserModel>>> GetUsers()
    {
        try { return Ok(await appUserAuthService.GetUsersAsync()); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
    }

    [HttpPost]
    public async Task<ActionResult<AppUserModel>> Create([FromBody] AppUserCreateRequest request)
    {
        try { return Ok(await appUserAuthService.CreateUserAsync(request)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<AppUserModel>> Update(long id, [FromBody] AppUserUpdateRequest request)
    {
        try { return Ok(await appUserAuthService.UpdateUserAsync(id, request)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        try { await appUserAuthService.DeleteUserAsync(id); return NoContent(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
