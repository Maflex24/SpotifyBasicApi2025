using System.Text.Json;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TokenManagerService _tokenManager;
        private readonly AuthService _authService;

        public SpotifyApiService(IHttpClientFactory httpClientFactory, TokenManagerService tokenManager, AuthService authService)
        {
            _httpClientFactory = httpClientFactory;
            _tokenManager = tokenManager;
            _authService = authService;
        }

        public async Task<object?> GetCurrentUserProfileAsync()
        {
            return await MakeSpotifyRequestAsync("me");
        }

        public async Task<object?> GetUserPlaylistsAsync()
        {
            return await MakeSpotifyRequestAsync("me/playlists");
        }

        public async Task<object?> SearchTracksAsync(string query, int limit = 20)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            return await MakeSpotifyRequestAsync($"search?q={encodedQuery}&type=track&limit={limit}");
        }

        public async Task<object?> GetFollowedArtists(int limit = 10)
        {
            return await MakeSpotifyRequestAsync($"me/following?type=artist&limit={limit}");
        }

        public async Task<object?> GetCurrentUserTopItems(string type = "tracks", int limit = 10)
        {
            return await MakeSpotifyRequestAsync($"me/top/{type}?limit={limit}");
        }

        public async Task<object?> GetUserSavedTracks(int limit = 10)
        {
            return await MakeSpotifyRequestAsync($"me/tracks?limit={limit}");
        }

        public async Task<object?> GetUserSavedAlbums(int limit = 10)
        {
            return await MakeSpotifyRequestAsync($"me/albums?limit={limit}");
        }

        public async Task<object?> GetCurrentUserDevices()
        {
            return await MakeSpotifyRequestAsync("me/player/devices");
        }

        public async Task<object?> GetCurrentPlaybackState()
        {
            return await MakeSpotifyRequestAsync("me/player");
        }

        private async Task<object?> MakeSpotifyRequestAsync(string endpoint)
        {
            var accessToken = _tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");
            }

            using var httpClient = _httpClientFactory.CreateClient("SpotifyApi");
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token wygasł, próbujemy odświeżyć
                var refreshedToken = await _authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    // Ponawiamy żądanie z nowym tokenem
                    request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);
                    response = await httpClient.SendAsync(request);
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }

            // Dodajemy logowanie błędów
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Spotify API Error: {response.StatusCode} - {errorContent}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException($"Brak uprawnień do endpointu {endpoint}. Sprawdź scope tokenu.");
            }

            return null;
        }
    }
}