using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly TokenManagerService _tokenManager;

        public AuthService(IConfiguration configuration, HttpClient httpClient, TokenManagerService tokenManager)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _tokenManager = tokenManager;
        }

        public string GetAuthorizationUrl()
        {
            var clientId = _configuration["Spotify:ClientId"];
            var redirectUri = _configuration["Spotify:RedirectUri"];
            var scope = "user-read-private user-read-email user-follow-read user-library-read playlist-read-private playlist-read-collaborative";

            var state = Guid.NewGuid().ToString();

            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUri!,
                ["state"] = state,
                ["scope"] = scope
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            return $"https://accounts.spotify.com/authorize?{queryString}";
        }

        public async Task<SpotifyTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];
            var redirectUri = _configuration["Spotify:RedirectUri"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri!
            };

            var content = new FormUrlEncodedContent(requestData);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = content
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent);

                if (tokenResponse != null)
                {
                    _tokenManager.SetToken(tokenResponse);
                }

                return tokenResponse;
            }

            return null;
        }



        public async Task<SpotifyTokenResponse?> RefreshTokenAsync(string refreshToken)
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(requestData);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = content
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent);

                if (tokenResponse != null)
                {
                    _tokenManager.SetToken(tokenResponse);
                }

                return tokenResponse;
            }

            return null;
        }

        public async Task<SpotifyTokenResponse?> RefreshCurrentTokenAsync()
        {
            var refreshToken = _tokenManager.GetRefreshToken();

            if (string.IsNullOrEmpty(refreshToken))
            {
                return null;
            }

            return await RefreshTokenAsync(refreshToken);
        }
    }
}