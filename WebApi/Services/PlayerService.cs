using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SpotifyBasicApi2025.Models;

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

        public async Task<PlaybackStartResult> PlayAsync(string? deviceId = null, object? body = null)
        {
            body = WithExplicitStartPosition(body);
            var devices = await SendAsync<PlayerDevices>(HttpMethod.Get, "me/player/devices", null, null);
            var available = devices?.Devices.Where(d => !d.IsRestricted && !string.IsNullOrWhiteSpace(d.Id)).ToList();
            var device = string.IsNullOrWhiteSpace(deviceId)
                ? available?.OrderByDescending(d => d.IsActive).FirstOrDefault()
                : available?.FirstOrDefault(d => d.Id == deviceId);

            if (device == null)
                throw new InvalidOperationException("Brak dostępnego urządzenia Spotify. Otwórz Spotify na telefonie lub komputerze i spróbuj ponownie.");

            var query = new Dictionary<string, string> { ["device_id"] = device.Id! };
            if (!device.IsActive)
                await ActivateDeviceAsync(device.Id!);

            await SendAsync<object?>(HttpMethod.Put, "me/player/play", query, body);
            if (await WaitForPlaybackAsync(device.Id!, body))
                return new();

            var payload = body == null ? default : JsonSerializer.SerializeToElement(body);
            if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("uris", out var tracks)
                && tracks.ValueKind == JsonValueKind.Array && tracks.GetArrayLength() > 0)
            {
                var uris = tracks.EnumerateArray().Select(track => track.GetString()!).ToList();
                return await PlayThroughAlbumAsync(device.Id!, uris, payload);
            }

            // 204 potwierdza przyjęcie polecenia, ale klient Spotify może go nie wykonać.
            // Ponownie aktywuj to samo urządzenie i dopiero potem wyślij żądany utwór/kontekst.
            await ActivateDeviceAsync(device.Id!);
            await SendAsync<object?>(HttpMethod.Put, "me/player/play", query, body);
            if (await WaitForPlaybackAsync(device.Id!, body))
                return new();

            throw new InvalidOperationException("Spotify przyjęło polecenie, ale nie rozpoczęło wybranego odtwarzania. Otwórz aplikację Spotify i spróbuj ponownie.");
        }

        public sealed record PlaybackStartResult(bool Started = true, string? Warning = null);

        private async Task<PlaybackStartResult> PlayThroughAlbumAsync(string deviceId, List<string> uris, JsonElement original)
        {
            // Obejście regresji klientów Spotify: samo uris potrafi zwrócić 204 i wyczyścić player.
            // https://community.spotify.com/forum/en/discussion/7552922
            const string trackPrefix = "spotify:track:";
            var firstUri = uris[0];
            if (string.IsNullOrWhiteSpace(firstUri) || !firstUri.StartsWith(trackPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Nie można uruchomić tego utworu przez kontekst albumu Spotify.");

            var trackId = Uri.EscapeDataString(firstUri[trackPrefix.Length..]);
            var track = await SendAsync<SpotifyTrackObject>(HttpMethod.Get, $"tracks/{trackId}", null, null);
            if (string.IsNullOrWhiteSpace(track?.Album?.Uri))
                throw new InvalidOperationException("Spotify nie zwróciło albumu potrzebnego do uruchomienia utworu.");

            var playableUri = string.IsNullOrWhiteSpace(track.Uri) ? firstUri : track.Uri;
            var body = new
            {
                context_uri = track.Album.Uri,
                offset = new { uri = playableUri },
                position_ms = original.TryGetProperty("position_ms", out var position) ? position.GetInt32() : 0
            };
            var query = new Dictionary<string, string> { ["device_id"] = deviceId };
            await SendAsync<object?>(HttpMethod.Put, "me/player/play", query, body);
            if (!await WaitForPlaybackAsync(deviceId, body))
                throw new InvalidOperationException("Spotify nie rozpoczęło utworu także przy odtwarzaniu przez album. Spróbuj wybrać Spotify Web Player jako urządzenie.");

            var queued = 0;
            foreach (var uri in uris.Skip(1))
            {
                try
                {
                    var queueQuery = new Dictionary<string, string> { ["device_id"] = deviceId, ["uri"] = uri };
                    await SendAsync<object?>(HttpMethod.Post, "me/player/queue", queueQuery, null);
                    queued++;
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
                {
                    return new(Warning: $"Odtwarzanie rozpoczęto przez album, ale do kolejki dodano tylko {queued} z {uris.Count - 1} pozostałych utworów. Spotify może kontynuować album. Szczegóły: {ex.Message}");
                }
            }

            return new(Warning: "Użyto obejścia błędu Spotify: utwór uruchomiono przez album, a pozostałe dodano do kolejki. Po końcu listy Spotify może kontynuować album; wcześniejsza ręczna kolejka może nadal być obecna.");
        }

        private static object? WithExplicitStartPosition(object? body)
        {
            if (body == null || JsonSerializer.SerializeToNode(body) is not JsonObject payload)
                return body;

            var contextUri = payload["context_uri"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(contextUri))
                return body;

            // Bez offsetu/czasu Spotify może wznowić zapamiętane miejsce w kontekście.
            // Pusty body nadal oznacza zwykłe wznowienie, a jawne pozycje pozostają bez zmian.
            if ((contextUri.StartsWith("spotify:album:", StringComparison.Ordinal)
                || contextUri.StartsWith("spotify:playlist:", StringComparison.Ordinal)) && !payload.ContainsKey("offset"))
                payload["offset"] = new JsonObject { ["position"] = 0 };
            if (!payload.ContainsKey("position_ms"))
                payload["position_ms"] = 0;
            return payload;
        }

        private async Task ActivateDeviceAsync(string deviceId)
        {
            await TransferAsync(new[] { deviceId }, play: false);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(300);
                var state = await SendAsync<PlayerState>(HttpMethod.Get, "me/player", null, null);
                if (state?.Device?.Id == deviceId && state.Device.IsActive)
                    return;
            }

            throw new InvalidOperationException("Urządzenie Spotify nie zostało aktywowane. Otwórz aplikację Spotify i spróbuj ponownie.");
        }

        private async Task<bool> WaitForPlaybackAsync(string deviceId, object? body)
        {
            var payload = body == null ? default : JsonSerializer.SerializeToElement(body);
            string? contextUri = null;
            string? trackUri = null;
            if (payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("context_uri", out var context))
                    contextUri = context.GetString();
                if (payload.TryGetProperty("uris", out var tracks) && tracks.ValueKind == JsonValueKind.Array && tracks.GetArrayLength() > 0)
                    trackUri = tracks[0].GetString();
                if (payload.TryGetProperty("offset", out var offset) && offset.ValueKind == JsonValueKind.Object
                    && offset.TryGetProperty("uri", out var offsetUri))
                    trackUri = offsetUri.GetString();
            }

            for (var attempt = 0; attempt < 6; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(500);
                var state = await SendAsync<PlayerState>(HttpMethod.Get, "me/player", null, null);
                if (state?.IsPlaying != true || state.Device?.Id != deviceId)
                    continue;
                if (!string.IsNullOrEmpty(contextUri) && state.Context?.Uri != contextUri)
                    continue;
                if (!string.IsNullOrEmpty(trackUri) && state.Item?.Uri != trackUri && state.Item?.LinkedFrom?.Uri != trackUri)
                    continue;
                return true;
            }
            return false;
        }

        private sealed class PlayerDevices
        {
            public List<PlayerDevice> Devices { get; set; } = new();
        }

        private sealed class PlayerDevice
        {
            public string? Id { get; set; }
            [JsonPropertyName("is_active")]
            public bool IsActive { get; set; }
            [JsonPropertyName("is_restricted")]
            public bool IsRestricted { get; set; }
        }

        private sealed class PlayerState
        {
            public PlayerDevice? Device { get; set; }
            [JsonPropertyName("is_playing")]
            public bool IsPlaying { get; set; }
            public PlaybackItem? Item { get; set; }
            public PlaybackItem? Context { get; set; }
        }

        private sealed class PlaybackItem
        {
            public string? Uri { get; set; }
            [JsonPropertyName("linked_from")]
            public PlaybackItem? LinkedFrom { get; set; }
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
