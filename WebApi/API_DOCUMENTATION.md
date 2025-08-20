## Auth

- GET `api/auth/login` → zwraca `authUrl`
- POST `api/auth/exchange-code` → body: string `code`, wymienia kod na tokeny i zapisuje w `TokenManagerService`
- GET `api/auth/current-token`
- POST `api/auth/refresh-current`
- DELETE `api/auth/clear-token`

Redirect URI ustaw: `http://localhost:5072/callback` (Blazor frontend)

# Spotify Basic API 2025 - Dokumentacja

## Przegląd

Ta aplikacja umożliwia autoryzację z Spotify i przechowywanie tokenu w pamięci aplikacji, dzięki czemu jest on dostępny dla wszystkich serwisów.

## Endpointy Autoryzacji

### 1. Rozpoczęcie logowania
```
GET /api/auth/login
```
Zwraca URL do autoryzacji Spotify.

### 2. Callback (automatyczny)
```
GET /callback?code={code}&state={state}
```
Obsługuje callback z Spotify, wymienia kod na token i zapisuje go w pamięci aplikacji.

### 3. Odświeżenie tokenu
```
POST /api/auth/refresh
Content-Type: application/json

"refresh_token_string"
```

### 4. Odświeżenie aktualnego tokenu
```
POST /api/auth/refresh-current
```
Odświeża token zapisany w pamięci aplikacji.

### 5. Pobranie aktualnego tokenu
```
GET /api/auth/current-token
```
Zwraca aktualnie zapisany token.

### 6. Wyczyszczenie tokenu
```
DELETE /api/auth/clear-token
```
Usuwa token z pamięci aplikacji.

## Endpointy Spotify API

### 1. Profil użytkownika
```
GET /api/spotify/profile
```
Pobiera profil aktualnie zalogowanego użytkownika.

### 2. Playlisty użytkownika
```
GET /api/spotify/playlists
```
Pobiera playlisty aktualnie zalogowanego użytkownika.

### 3. Wyszukiwanie utworów
```
GET /api/spotify/search?q={query}&limit={limit}
```
Wyszukuje utwory na podstawie podanego zapytania.

## Jak to działa

1. **Autoryzacja**: Użytkownik przechodzi przez proces OAuth2 z Spotify
2. **Zapisywanie tokenu**: Po otrzymaniu kodu autoryzacji, aplikacja wymienia go na token dostępu i zapisuje w `TokenManagerService`
3. **Dostęp w serwisach**: Wszystkie serwisy mogą uzyskać dostęp do tokenu poprzez wstrzyknięcie `TokenManagerService`
4. **Automatyczne odświeżanie**: Gdy token wygasa, serwisy automatycznie próbują go odświeżyć

## Przykład użycia w serwisie

```csharp
public class MyService
{
    private readonly TokenManagerService _tokenManager;
    private readonly AuthService _authService;

    public MyService(TokenManagerService tokenManager, AuthService authService)
    {
        _tokenManager = tokenManager;
        _authService = authService;
    }

    public async Task<object> CallSpotifyApi()
    {
        var accessToken = _tokenManager.GetAccessToken();
        
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Brak tokenu dostępu");
        }

        // Użyj tokenu do wywołania Spotify API
        // ...
    }
}
```

## Bezpieczeństwo

- Token jest przechowywany w pamięci aplikacji (nie w bazie danych)
- Token automatycznie wygasa po określonym czasie
- Aplikacja automatycznie odświeża token gdy wygasa
- Token można wyczyścić za pomocą endpointu DELETE

## Testowanie

1. Uruchom aplikację
2. Przejdź do `http://127.0.0.1:8888/api/auth/login`
3. Zaloguj się przez Spotify
4. Po callbacku token zostanie zapisany
5. Użyj endpointów `/api/spotify/*` do testowania API

