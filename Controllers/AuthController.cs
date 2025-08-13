using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [Route("api/auth")]
    [Authorize]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService authService;

        public AuthController(AuthService authService)
        {
            this.authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("spotify")]
        public async Task<ActionResult<string>> Login()
        {
            var token = authService.ReturnToken();

            return Ok(token);
        }
    }
}