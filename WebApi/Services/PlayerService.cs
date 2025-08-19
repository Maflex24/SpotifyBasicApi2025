using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SpotifyBasicApi2025.Services
{
    public class PlayerService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TokenManagerService tokenManager;
        private readonly AuthService authService;

        public PlayerService(IHttpClientFactory httpClientFactory, TokenManagerService tokenManager, AuthService authService)
        {
            this.httpClientFactory = httpClientFactory;
            this.tokenManager = tokenManager;
            this.authService = authService;
        }

        public async Task<object?> GetPlaybackStateAsync(string? market = null)
        {
            var query = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(market))
                query["market"] = market;

            return await SendAsync<object?>(HttpMethod.Get, "me/player", query, null);
        }

        public async Task<bool> PlayAsync(string? deviceId = null, object? body = null)
        {
            var query = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            var response = await SendAsync<object?>(HttpMethod.Put, "me/player/play", query, body);

            return response != null || true;
        }

        public async Task<bool> PauseAsync(string? deviceId = null)
        {
            var query = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            await SendAsync<object?>(HttpMethod.Put, "me/player/pause", query, null);

            return true;
        }

        public async Task<bool> NextAsync(string? deviceId = null)
        {
            var query = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            await SendAsync<object?>(HttpMethod.Post, "me/player/next", query, null);

            return true;
        }

        public async Task<bool> PreviousAsync(string? deviceId = null)
        {
            var query = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            await SendAsync<object?>(HttpMethod.Post, "me/player/previous", query, null);

            return true;
        }

        public async Task<bool> SeekAsync(int positionMs, string? deviceId = null)
        {
            var query = new Dictionary<string, string>
            {
                ["position_ms"] = positionMs.ToString()
            };

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            await SendAsync<object?>(HttpMethod.Put, "me/player/seek", query, null);

            return true;
        }

        public async Task<bool> SetVolumeAsync(int volumePercent, string? deviceId = null)
        {
            var query = new Dictionary<string, string>
            {
                ["volume_percent"] = Math.Clamp(volumePercent, 0, 100).ToString()
            };

            if (!string.IsNullOrEmpty(deviceId))
                query["device_id"] = deviceId;

            await SendAsync<object?>(HttpMethod.Put, "me/player/volume", query, null);

            return true;
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string path, Dictionary<string, string>? query, object? body)
        {
            var accessToken = tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

            using var httpClient = httpClientFactory.CreateClient("SpotifyApi");

            var uriBuilder = new StringBuilder(path);

            if (query != null && query.Count > 0)
            {
                var q = string.Join('&', query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                uriBuilder.Append('?');
                uriBuilder.Append(q);
            }

            var request = new HttpRequestMessage(method, uriBuilder.ToString());
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshedToken = await authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    request = new HttpRequestMessage(method, uriBuilder.ToString());
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);

                    if (body != null)
                    {
                        var jsonRetry = JsonSerializer.Serialize(body);
                        request.Content = new StringContent(jsonRetry, Encoding.UTF8, "application/json");
                    }

                    response = await httpClient.SendAsync(request);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                // 204 no content jest sukcesem dla wielu operacji playera
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Spotify Player API Error: {response.StatusCode} - {errorContent}");
            }

            if (response.Content == null)
                return default;

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}


