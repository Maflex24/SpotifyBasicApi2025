using System.Text.Json;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenManagerService _tokenManager;
        private readonly AuthService _authService;

        public SpotifyApiService(HttpClient httpClient, TokenManagerService tokenManager, AuthService authService)
        {
            _httpClient = httpClient;
            _tokenManager = tokenManager;
            _authService = authService;
        }

        public async Task<object?> GetCurrentUserProfileAsync()
        {
            var accessToken = _tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token wygasł, próbujemy odświeżyć
                var refreshedToken = await _authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    // Ponawiamy żądanie z nowym tokenem
                    request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);
                    response = await _httpClient.SendAsync(request);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }

            return null;
        }

        public async Task<object?> GetUserPlaylistsAsync()
        {
            var accessToken = _tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token wygasł, próbujemy odświeżyć
                var refreshedToken = await _authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    // Ponawiamy żądanie z nowym tokenem
                    request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);
                    response = await _httpClient.SendAsync(request);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }

            return null;
        }

        public async Task<object?> SearchTracksAsync(string query, int limit = 20)
        {
            var accessToken = _tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");
            }

            var encodedQuery = Uri.EscapeDataString(query);
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit={limit}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token wygasł, próbujemy odświeżyć
                var refreshedToken = await _authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    // Ponawiamy żądanie z nowym tokenem
                    request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit={limit}");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);
                    response = await _httpClient.SendAsync(request);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }

            return null;
        }
    }
}

