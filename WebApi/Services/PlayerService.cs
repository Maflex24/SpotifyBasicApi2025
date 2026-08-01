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

        public async Task<object?> GetDevicesAsync()
        {
            return await SendAsync<object?>(HttpMethod.Get, "me/player/devices", null, null);
        }

        public async Task<bool> TransferAsync(string[] deviceIds, bool? play = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["device_ids"] = deviceIds
            };

            if (play.HasValue)
                body["play"] = play.Value;

            await SendAsync<object?>(HttpMethod.Put, "me/player", null, body);

            return true;
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

        public async Task<object> QueueAlbumAsync(string albumId, IEnumerable<string?> trackUris)
        {
            var uris = trackUris
                .Where(uri => !string.IsNullOrWhiteSpace(uri))
                .Select(uri => uri!)
                .ToList();

            if (uris.Count == 0)
                throw new InvalidOperationException("Album nie zawiera utworów, które można dodać do kolejki.");

            var playbackQueue = await SendAsync<PlaybackQueueResponse>(HttpMethod.Get, "me/player/queue", null, null);
            var lastQueuedAlbumId = playbackQueue?.Queue.LastOrDefault()?.Album?.Id;

            if (string.Equals(lastQueuedAlbumId, albumId, StringComparison.Ordinal))
            {
                return new { added = false, tracksAdded = 0, message = "Album jest już ostatni w kolejce." };
            }

            var tracksAdded = 0;
            var skippedTracks = 0;

            foreach (var uri in uris)
            {
                try
                {
                    var query = new Dictionary<string, string> { ["uri"] = uri };
                    await SendAsync<object?>(HttpMethod.Post, "me/player/queue", query, null);
                    tracksAdded++;

                    // Spotify kolejkuje tylko pojedyncze utwory. Krótka przerwa zachowuje
                    // kolejność poleceń i ogranicza ryzyko natrafienia na rate limit.
                    await Task.Delay(TimeSpan.FromMilliseconds(75));
                }
                catch (InvalidOperationException)
                {
                    // Pojedynczy niedostępny utwór nie może zatrzymać dodawania reszty albumu.
                    skippedTracks++;
                }
            }

            if (tracksAdded == 0)
                throw new InvalidOperationException("Nie udało się dodać żadnego utworu z albumu do kolejki.");

            return new { added = true, tracksAdded, skippedTracks };
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

        private sealed class PlaybackQueueResponse
        {
            public List<PlaybackQueueItem> Queue { get; set; } = new();
        }

        private sealed class PlaybackQueueItem
        {
            public PlaybackQueueAlbum? Album { get; set; }
        }

        private sealed class PlaybackQueueAlbum
        {
            public string? Id { get; set; }
        }

        private async Task<T?> SendAsync<T>(HttpMethod method, string path, Dictionary<string, string>? query, object? body)
        {
            if (tokenManager.IsExpiringSoon(60))
                await authService.RefreshCurrentTokenAsync();

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

            for (var retryAttempt = 0; (int)response.StatusCode == 429 && retryAttempt < 5; retryAttempt++)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                await Task.Delay(retryAfter + TimeSpan.FromMilliseconds(100));

                request = new HttpRequestMessage(method, uriBuilder.ToString());
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenManager.GetAccessToken());

                if (body != null)
                {
                    var jsonRetry = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(jsonRetry, Encoding.UTF8, "application/json");
                }

                response = await httpClient.SendAsync(request);
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

                throw new InvalidOperationException($"Spotify API error: {(int)response.StatusCode} {response.ReasonPhrase} - {errorContent}");
            }

            // Polecenia playera (POST/PUT) nie zwracają danych, z których korzystamy.
            // Spotify może zwrócić niepustą treść mimo poprawnego statusu, która nie musi być JSON-em.
            if (method != HttpMethod.Get)
                return default;

            if (response.Content == null)
                return default;

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
