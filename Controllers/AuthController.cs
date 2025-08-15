using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Models;

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

        public CallbackController(AuthService authService, TokenManagerService tokenManager)
        {
            _authService = authService;
            _tokenManager = tokenManager;
        }

        [HttpGet]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Brak kodu autoryzacji");
            }

            var tokenResponse = await _authService.ExchangeCodeForTokenAsync(code);

            if (tokenResponse == null)
            {
                return BadRequest("Nie udało się uzyskać tokenu");
            }

            // Zwracamy stronę HTML z informacją o sukcesie
            var html = $@"
<!DOCTYPE html>
<html lang='pl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Autoryzacja Spotify - Sukces</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .success {{
            background-color: #d4edda;
            border: 1px solid #c3e6cb;
            color: #155724;
            padding: 15px;
            border-radius: 5px;
            margin: 10px 0;
        }}
        .token-info {{
            background-color: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 5px;
            padding: 15px;
            margin: 10px 0;
            font-family: monospace;
            white-space: pre-wrap;
            word-break: break-all;
        }}
        button {{
            background-color: #1DB954;
            color: white;
            border: none;
            padding: 15px 30px;
            border-radius: 25px;
            font-size: 16px;
            cursor: pointer;
            margin: 10px 0;
        }}
        button:hover {{
            background-color: #1ed760;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🎉 Autoryzacja Spotify zakończona sukcesem!</h1>
        
        <div class='success'>
            <strong>Token dostępu został pomyślnie uzyskany.</strong>
        </div>

        <h3>Informacje o tokenie:</h3>
        <div class='token-info'>
Access Token: {tokenResponse.AccessToken}

Refresh Token: {tokenResponse.RefreshToken}

Token Type: {tokenResponse.TokenType}

Expires In: {tokenResponse.ExpiresIn} sekund
        </div>

        <div class='success'>
            <strong>✅ Token został zapisany w pamięci aplikacji i jest dostępny dla wszystkich serwisów!</strong>
        </div>

        <h3>Przykłady użycia:</h3>
        <p>Możesz teraz używać tego tokenu do wywoływania Spotify Web API:</p>
        
        <button onclick='copyToken()'>Skopiuj Access Token</button>
        <button onclick='window.location.href=""http://127.0.0.1:8888/index.html""'>Powrót do strony głównej</button>

        <h3>Testowanie API:</h3>
        <p>Sprawdź czy token działa poprawnie:</p>
        <button onclick='testApi(""profile"")'>Pobierz profil użytkownika</button>
        <button onclick='testApi(""playlists"")'>Pobierz playlisty</button>
        <button onclick='testApi(""search?q=test"")'>Wyszukaj utwory</button>

        <script>
            function copyToken() {{
                navigator.clipboard.writeText('{tokenResponse.AccessToken}').then(function() {{
                    alert('Access Token skopiowany do schowka!');
                }});
            }}

            function testApi(endpoint) {{
                fetch(`http://127.0.0.1:8888/api/spotify/${{endpoint}}`)
                    .then(response => response.json())
                    .then(data => {{
                        alert('API działa poprawnie! Sprawdź konsolę przeglądarki dla szczegółów.');
                        console.log('API Response:', data);
                    }})
                    .catch(error => {{
                        alert('Błąd API: ' + error.message);
                        console.error('API Error:', error);
                    }});
            }}
        </script>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}