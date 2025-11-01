# Analiza logowania i tokenów w Spotify
_Exported on 11/1/2025 at 11:23:13 GMT+1 from Cursor (1.7.54)_

---

**User**

Przeanalizuj sposób logowania do spotify i przechowywanie/odświeżanie tokenu. Chodzi mi o to, że po pewnym, stosunkowo krótkim czasie aplikacja mnie wylogowuje, ale tego w ogóle nie widać, orientuję się dopiero, gdy chcę coś zrobić, ale nie ma efektu tej akcji. Czy możemy ten czas do automatycznego wylogowania mocno wydłużyć, lub używać jakiś refresh token?

---

**Cursor**

Szybka kontrola repo — poszukam plików i fragmentów związanych z logowaniem do Spotify, tokenami i refresh tokenami (przeszukam cały projekt). Zaraz odczytam wyniki.

Krótka diagnoza przed dalszą analizą: sprawdziłem serwis autoryzacji i menedżera tokenów w backendzie — poniżej wnioski i proponowane zmiany (kod do wklejenia/edytowania).

Co znalazłem (odwołania do istniejącego kodu)
- `TokenManagerService` przechowuje token tylko w pamięci i nie zapisuje czasu wygaśnięcia:
```startLine:endLine:WebApi/Services/TokenManagerService.cs
1:59:WebApi/Services/TokenManagerService.cs
```
- `AuthService.RefreshTokenAsync` odświeża token i nadpisuje zapis w `TokenManagerService`:
```startLine:endLine:WebApi/Services/AuthService.cs
103:155:WebApi/Services/AuthService.cs
```
- `SpotifyRequestService` / `PlayerService` wykonują żądanie i w przypadku 401 próbują odświeżyć token:
```startLine:endLine:WebApi/Services/SpotifyRequestService.cs
1:39:WebApi/Services/SpotifyRequestService.cs
```
```startLine:endLine:WebApi/Services/PlayerService.cs
121:166:WebApi/Services/PlayerService.cs
```

Główne problemy (dlaczego aplikacja "cicho" się wylogowuje)
- Access token od Spotify ma ograniczony czas życia (zwykle ~3600s) i nie można go wydłużyć po stronie klienta — trzeba używać refresh token.
- Refresh token jest zwracany tylko przy pierwszej wymianie kodu; przy odświeżeniu Spotify często nie zwraca nowego `refresh_token`.
- Aktualna implementacja w `AuthService.RefreshTokenAsync` nadpisuje cały obiekt tokenu zwróconym `tokenResponse`. Jeśli nowe `tokenResponse.RefreshToken` jest null, nadpisanie usuwa zapisany refresh token — potem nie można już ponownie odświeżyć (następne wygaśnięcie powoduje utratę możliwości automatycznego odświeżenia).
- Tokeny są trzymane tylko w pamięci procesu (aplikacja restart -> wylogowanie). Brak informacji o czasie uzyskania/wygaśnięcia tokenu uniemożliwia proaktywne odświeżanie przed 401.

Co proponuję (szybkie, bezpieczne zmiany do wklejenia)

1) Zmiana `TokenManagerService` — przechowywać czas wygaśnięcia i przy SetToken zachować istniejący `refresh_token` gdy nowy go nie zawiera; dodać metodę `IsExpiringSoon`.

Wklej (zamień zawartość `WebApi/Services/TokenManagerService.cs` na poniższe):

```csharp
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class TokenManagerService
    {
        private SpotifyTokenResponse? _currentToken;
        private DateTimeOffset? _expiresAt;
        private readonly object _lockObject = new object();

        public SpotifyTokenResponse? GetCurrentToken()
        {
            lock (_lockObject)
            {
                return _currentToken;
            }
        }

        public void SetToken(SpotifyTokenResponse token)
        {
            lock (_lockObject)
            {
                if (token.RefreshToken == null && _currentToken?.RefreshToken != null)
                    token.RefreshToken = _currentToken.RefreshToken;

                _currentToken = token;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            }
        }

        public void ClearToken()
        {
            lock (_lockObject)
            {
                _currentToken = null;
                _expiresAt = null;
            }
        }

        public bool HasValidToken()
        {
            lock (_lockObject)
            {
                return _currentToken != null && !string.IsNullOrEmpty(_currentToken.AccessToken);
            }
        }

        public string? GetAccessToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.AccessToken;
            }
        }

        public string? GetRefreshToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.RefreshToken;
            }
        }

        public bool IsExpiringSoon(int seconds = 60)
        {
            lock (_lockObject)
            {
                if (!_expiresAt.HasValue)
                    return false;
                return DateTimeOffset.UtcNow.AddSeconds(seconds) >= _expiresAt.Value;
            }
        }

        public DateTimeOffset? GetExpiresAt()
        {
            lock (_lockObject)
            {
                return _expiresAt;
            }
        }
    }
}
```

2) Zmiana `AuthService.RefreshTokenAsync` — jeśli odpowiedź od Spotify nie zawiera `refresh_token`, zachowaj poprzedni refresh token przed ustawieniem w `TokenManagerService`.

Wklej zmodyfikowaną funkcję (zastąp istniejącą funkcję `RefreshTokenAsync` w `WebApi/Services/AuthService.cs`):

```csharp
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
            if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
                tokenResponse.RefreshToken = _tokenManager.GetRefreshToken();

            _tokenManager.SetToken(tokenResponse);
        }

        return tokenResponse;
    }

    return null;
}
```

3) Proaktywne odświeżanie przed żądaniem — w `SpotifyRequestService.MakeSpotifyRequestAsync` (i analogicznie w `PlayerService.SendAsync`) przed użyciem access token sprawdź `IsExpiringSoon()` i spróbuj odświeżyć:

Wklej fragment (zmodyfikuj początek funkcji `MakeSpotifyRequestAsync`):

```csharp
public async Task<object?> MakeSpotifyRequestAsync(string endpoint)
{
    if (tokenManager.IsExpiringSoon(60))
    {
        await authService.RefreshCurrentTokenAsync();
    }

    var accessToken = tokenManager.GetAccessToken();

    if (string.IsNullOrEmpty(accessToken))
        throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

    // ... dalsza logika wysyłania żądania ...
}
```

Zastosuj podobną zmianę w `PlayerService.SendAsync` (tam już jest logika 401 → odśwież; dodać przedwywołanie `IsExpiringSoon`).

Dodatkowe rekomendacje (opcjonalne, ale zalecane)
- Nie da się technicznie wydłużyć lifetime access tokena po stronie aplikacji — trzeba użyć refresh tokenów, tak jak powyżej.
- Jeśli chcesz uniknąć wylogowania po restarcie procesu, zapisz refresh token w bezpiecznym miejscu (np. plik konfiguracyjny poza repo, Windows DPAPI, Azure Key Vault itp.). Obecne trzymanie tylko w pamięci powoduje utratę sesji przy restarcie aplikacji.
- Dodaj logowanie stanu (info/error) przy odświeżaniu tokenów, żeby widzieć dlaczego żądania kończą się niepowodzeniem.

Krótka podsumowana lista zmian do wdrożenia
- **TokenManagerService**: przechowywanie czasu wygaśnięcia + zachowanie refresh token przy odświeżeniu + metoda `IsExpiringSoon`.
- **AuthService.RefreshTokenAsync**: jeśli Spotify nie zwróci nowego `refresh_token`, użyć poprzedniego przed zapisaniem.
- **SpotifyRequestService / PlayerService**: proaktywne odświeżanie jeśli token bliski wygaśnięcia + istniejąca logika odświeżania przy 401.

Jeżeli chcesz, mogę:
- przygotować dokładny patch (edytowany plik po pliku) do wklejenia,
- albo wdrożyć dodatkowe zapisanie refresh tokena na dysku (pokazać bezpieczną, prostą wersję).

Chcesz, żebym przygotował gotowe edity (pliki z dokładną ścieżką i zawartością) czy wolisz sam wkleić powyższe zmiany?

```plaintext
1:59:WebApi/Services/TokenManagerService.cs
```

```plaintext
103:155:WebApi/Services/AuthService.cs
```

```plaintext
1:39:WebApi/Services/SpotifyRequestService.cs
```

```plaintext
121:166:WebApi/Services/PlayerService.cs
```

```csharp
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class TokenManagerService
    {
        private SpotifyTokenResponse? _currentToken;
        private DateTimeOffset? _expiresAt;
        private readonly object _lockObject = new object();

        public SpotifyTokenResponse? GetCurrentToken()
        {
            lock (_lockObject)
            {
                return _currentToken;
            }
        }

        public void SetToken(SpotifyTokenResponse token)
        {
            lock (_lockObject)
            {
                if (token.RefreshToken == null && _currentToken?.RefreshToken != null)
                    token.RefreshToken = _currentToken.RefreshToken;

                _currentToken = token;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            }
        }

        public void ClearToken()
        {
            lock (_lockObject)
            {
                _currentToken = null;
                _expiresAt = null;
            }
        }

        public bool HasValidToken()
        {
            lock (_lockObject)
            {
                return _currentToken != null && !string.IsNullOrEmpty(_currentToken.AccessToken);
            }
        }

        public string? GetAccessToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.AccessToken;
            }
        }

        public string? GetRefreshToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.RefreshToken;
            }
        }

        public bool IsExpiringSoon(int seconds = 60)
        {
            lock (_lockObject)
            {
                if (!_expiresAt.HasValue)
                    return false;
                return DateTimeOffset.UtcNow.AddSeconds(seconds) >= _expiresAt.Value;
            }
        }

        public DateTimeOffset? GetExpiresAt()
        {
            lock (_lockObject)
            {
                return _expiresAt;
            }
        }
    }
}
```

```csharp
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
            if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
                tokenResponse.RefreshToken = _tokenManager.GetRefreshToken();

            _tokenManager.SetToken(tokenResponse);
        }

        return tokenResponse;
    }

    return null;
}
```

```csharp
public async Task<object?> MakeSpotifyRequestAsync(string endpoint)
{
    if (tokenManager.IsExpiringSoon(60))
    {
        await authService.RefreshCurrentTokenAsync();
    }

    var accessToken = tokenManager.GetAccessToken();

    if (string.IsNullOrEmpty(accessToken))
        throw new InvalidOperationException("Brak tokenu dostępu. Najpierw musisz się zalogować.");

    // ... dalsza logika wysyłania żądania ...
}
```

---

**User**

Zastosuj podobną zmianę w PlayerService.SendAsync (tam już jest logika 401 → odśwież; dodać przedwywołanie IsExpiringSoon). - obecne zmiany nie wystarczą, aby token się odświeżał przy tym endpoincie? Jeśli nie to zaproponuj zmiany

---

**Cursor**

Zaraz odczytam aktualną implementację `PlayerService.SendAsync`, żeby dokładnie wstawić proaktywne odświeżanie tokenu przed wysłaniem żądania.

Najpierw sprawdziłem `PlayerService.SendAsync`, teraz wrzucam propozycję zamiany tej metody — dodałem proaktywne odświeżanie, które wywołuje `RefreshCurrentTokenAsync()` jeśli token jest bliski wygaśnięcia. Obecna logika 401 nadal zostaje jako fallback, ale bez proaktywnego odświeżania niektóre akcje mogą wykonać się z niewidocznym brakiem efektu.

Zastąp metodę `SendAsync` w `WebApi/Services/PlayerService.cs` poniższym kodem:

```csharp
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

    if (response.Content == null)
        return default;

    var content = await response.Content.ReadAsStringAsync();

    if (string.IsNullOrWhiteSpace(content))
        return default;

    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
```

Krótka uwaga techniczna
- Sama logika 401 jest dobra jako fallback, ale jeśli token wygaśnie tuż przed żądaniem, bez proaktywnego odświeżania żądanie pójdzie z przestarzałym tokenem i trzeba będzie polegać na retry w przypadku 401 — to działa, ale proaktywne odświeżanie zmniejsza liczbę nieudanych prób i „ciche” brakujące efekty.
- Ważne: upewnij się, że wcześniej zastosowałeś poprawkę w `AuthService.RefreshTokenAsync` (zachowanie starego `refresh_token`, gdy Spotify nie zwróci nowego) oraz zmianę w `TokenManagerService` (przechowywanie expiresAt i IsExpiringSoon). Bez tego odświeżanie może nie działać poprawnie.

Chcesz, żebym przygotował gotowy patch z wszystkimi trzema plikami (`TokenManagerService`, `AuthService`, `PlayerService`) do wklejenia? 

- Zmiana w `PlayerService.SendAsync`: dodałem proaktywne odświeżanie przed wysłaniem żądania.
- Rekomendacja: konieczne są też poprawki w `AuthService` i `TokenManagerService` (opisane wcześniej) żeby odświeżanie działało pewnie.

```csharp
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

    if (response.Content == null)
        return default;

    var content = await response.Content.ReadAsStringAsync();

    if (string.IsNullOrWhiteSpace(content))
        return default;

    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
```

