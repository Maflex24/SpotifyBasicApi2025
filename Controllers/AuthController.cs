using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly TokenManagerService _tokenManager;

        public AuthController(AuthService authService, TokenManagerService tokenManager)
        {
            _authService = authService;
            _tokenManager = tokenManager;
        }

        [HttpGet("login")]
        public ActionResult<string> Login()
        {
            var authUrl = _authService.GetAuthorizationUrl();
            return Ok(new { authUrl });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<SpotifyTokenResponse>> RefreshToken([FromBody] string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Brak refresh token");
            }

            var tokenResponse = await _authService.RefreshTokenAsync(refreshToken);

            if (tokenResponse == null)
            {
                return BadRequest("Nie udało się odświeżyć tokenu");
            }

            return Ok(tokenResponse);
        }

        [HttpPost("refresh-current")]
        public async Task<ActionResult<SpotifyTokenResponse>> RefreshCurrentToken()
        {
            var tokenResponse = await _authService.RefreshCurrentTokenAsync();

            if (tokenResponse == null)
            {
                return BadRequest("Brak zapisanego refresh token lub nie udało się odświeżyć tokenu");
            }

            return Ok(tokenResponse);
        }

        [HttpGet("current-token")]
        public ActionResult<SpotifyTokenResponse> GetCurrentToken()
        {
            var token = _tokenManager.GetCurrentToken();

            if (token == null)
            {
                return NotFound("Brak zapisanego tokenu");
            }

            return Ok(token);
        }

        [HttpDelete("clear-token")]
        public ActionResult ClearToken()
        {
            _tokenManager.ClearToken();
            return Ok("Token został wyczyszczony");
        }
    }

    [Route("callback")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly TokenManagerService _tokenManager;
        private readonly SpotifyApiService _spotifyApiService;

        public CallbackController(AuthService authService, TokenManagerService tokenManager, SpotifyApiService spotifyApiService)
        {
            _authService = authService;
            _tokenManager = tokenManager;
            _spotifyApiService = spotifyApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string code, string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest("Brak kodu autoryzacji");
                }

                // Wymień kod na tokeny
                var tokenResponse = await _authService.ExchangeCodeForTokenAsync(code);

                if (tokenResponse == null)
                {
                    return BadRequest("Nie udało się uzyskać tokenów");
                }

                // Token jest już zapisany w TokenManagerService przez AuthService

                // Pobierz profil użytkownika
                var userProfile = await _spotifyApiService.GetCurrentUserProfileAsync();

                if (userProfile != null)
                {
                    var savedUser = await _spotifyApiService.SaveUserFromSpotifyProfileAsync(userProfile);
                    Console.WriteLine($"Użytkownik {savedUser.DisplayName} (ID: {savedUser.Id}) został zapisany w bazie danych");
                }

                return Ok(new { message = "Autoryzacja zakończona pomyślnie", user = userProfile });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas callback: {ex.Message}");
                return StatusCode(500, "Wystąpił błąd podczas autoryzacji");
            }
        }
    }
}