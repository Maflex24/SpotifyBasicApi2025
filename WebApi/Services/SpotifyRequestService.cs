using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyRequestService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly TokenManagerService tokenManager;
        private readonly AuthService authService;

        public SpotifyRequestService(IHttpClientFactory httpClientFactory, TokenManagerService tokenManager, AuthService authService)
        {
            this.httpClientFactory = httpClientFactory;
            this.tokenManager = tokenManager;
            this.authService = authService;
        }

        public async Task<object?> MakeSpotifyRequestAsync(string endpoint)
        {
            if (tokenManager.IsExpiringSoon(60))
            {
                await authService.RefreshCurrentTokenAsync();
            }

            var accessToken = tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

            using var httpClient = httpClientFactory.CreateClient("SpotifyApi");
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token wygasł, próbujemy odświeżyć
                var refreshedToken = await authService.RefreshCurrentTokenAsync();

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

        public async Task<bool> SendAsync(System.Net.Http.HttpMethod method, string endpoint, object? body = null)
        {
            var accessToken = tokenManager.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

            using var httpClient = httpClientFactory.CreateClient("SpotifyApi");

            var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            }

            var response = await httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshedToken = await authService.RefreshCurrentTokenAsync();

                if (refreshedToken != null)
                {
                    request = new HttpRequestMessage(method, endpoint);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshedToken.AccessToken);
                    if (body != null)
                        request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                    response = await httpClient.SendAsync(request);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Spotify API Error: {response.StatusCode} - {errorContent}");
                return false;
            }

            return true;
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            // Eager refresh, gdy token się kończy
            if (tokenManager.IsExpiringSoon(60))
            {
                await authService.RefreshCurrentTokenAsync();
            }

            var accessToken = tokenManager.GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

            using var httpClient = httpClientFactory.CreateClient("SpotifyApi");

            for (int attempts = 0; ; attempts++)
            {
                ct.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.SendAsync(request, ct);

                // 401 -> spróbuj 1x odświeżyć token
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempts < 1)
                {
                    var refreshedToken = await authService.RefreshCurrentTokenAsync();
                    if (refreshedToken != null)
                    {
                        accessToken = refreshedToken.AccessToken;
                        continue; // spróbujemy ponownie z nowym tokenem
                    }
                }

                // 429 -> uszanuj Retry-After
                if ((int)response.StatusCode == 429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                    if (attempts >= 5)
                        throw new HttpRequestException($"Zbyt wiele prób po 429 dla: {endpoint}");
                    await Task.Delay(retryAfter + TimeSpan.FromMilliseconds(100), ct);
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(ct);
                    return await JsonSerializer.DeserializeAsync<T>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        ct);
                }

                var errorContent = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Spotify API Error: {response.StatusCode} - {errorContent}");
            }
        }
    }
}