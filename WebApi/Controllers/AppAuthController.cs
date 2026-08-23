using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers;

[ApiController]
[Route("api/app-auth")]
public sealed class AppAuthController(AppUserAuthService appUserAuthService, UserService userService, TokenManagerService tokenManager) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SystemSessionStatusModel>> Status()
    {
        var appUser = await appUserAuthService.GetCurrentUserAsync();
        var spotifyUser = tokenManager.HasValidToken() ? await userService.GetCurrentUserAsync() : null;
        return Ok(new SystemSessionStatusModel
        {
            SetupRequired = await appUserAuthService.IsSetupRequiredAsync(),
            AppUser = new AppUserSessionStatus { IsLoggedIn = appUser != null, Nick = appUser?.Nick, IsAdmin = appUser?.IsAdmin ?? false },
            Spotify = new SpotifySessionStatus { IsLoggedIn = spotifyUser != null, DisplayName = spotifyUser?.DisplayName, Id = spotifyUser?.Id }
        });
    }

    [HttpPost("setup")]
    public async Task<ActionResult<AppUserModel>> Setup([FromBody] AppUserCredentialsRequest request)
    {
        try { return Ok(ToModel(await appUserAuthService.SetupAsync(request))); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppUserModel>> Login([FromBody] AppUserCredentialsRequest request)
    {
        var user = await appUserAuthService.LoginAsync(request);
        return user == null ? Unauthorized("Nieprawidłowy nick lub hasło.") : Ok(ToModel(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await appUserAuthService.LogoutAsync();
        return NoContent();
    }

    private static AppUserModel ToModel(Entities.AppUser user) => new() { Id = user.Id, Nick = user.Nick, IsAdmin = user.IsAdmin, CreatedAtUtc = user.CreatedAtUtc };
}
