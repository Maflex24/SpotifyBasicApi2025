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

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
    }

    [Route("callback")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly AuthService _authService;

        public CallbackController(AuthService authService)
        {
            _authService = authService;
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

        <h3>Przykłady użycia:</h3>
        <p>Możesz teraz używać tego tokenu do wywoływania Spotify Web API:</p>
        
        <button onclick='copyToken()'>Skopiuj Access Token</button>
        <button onclick='window.location.href=""http://127.0.0.1:8888/index.html""'>Powrót do strony głównej</button>

        <script>
            function copyToken() {{
                navigator.clipboard.writeText('{tokenResponse.AccessToken}').then(function() {{
                    alert('Access Token skopiowany do schowka!');
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