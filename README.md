# Spotify Basic API 2025

Aplikacja ASP.NET Core do autoryzacji i korzystania z API Spotify.

## Konfiguracja

### 1. Utwórz aplikację w Spotify Developer Dashboard

1. Przejdź do [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Zaloguj się na swoje konto Spotify
3. Kliknij "Create App"
4. Wypełnij formularz:
   - **App name**: Nazwa Twojej aplikacji
   - **App description**: Opis aplikacji
   - **Website**: `http://127.0.0.1:8888`
   - **Redirect URI**: `http://127.0.0.1:8888/callback`
   - **API/SDKs**: Wybierz "Web API"

**WAŻNE**: Spotify wymaga używania adresów loopback (`127.0.0.1`) zamiast `localhost` dla lokalnego rozwoju. Adresy `localhost` nie są już obsługiwane.

### 2. Skopiuj dane aplikacji

Po utworzeniu aplikacji, skopiuj:
- **Client ID**
- **Client Secret**

### 3. Zaktualizuj konfigurację

W pliku `appsettings.json` zastąp:
```json
"Spotify": {
  "ClientId": "YOUR_SPOTIFY_CLIENT_ID",
  "ClientSecret": "YOUR_SPOTIFY_CLIENT_SECRET",
  "RedirectUri": "http://127.0.0.1:8888/callback"
}
```

swoimi danymi z Spotify Developer Dashboard.

## Uruchomienie

1. Uruchom aplikację:
```bash
dotnet run
```

2. Otwórz przeglądarkę i przejdź do `http://127.0.0.1:8888`

3. Kliknij "Zaloguj się do Spotify" aby rozpocząć proces autoryzacji

## Endpointy API

### GET /api/auth/login
Zwraca URL do autoryzacji Spotify.

### GET /callback
Endpoint callback dla Spotify OAuth. Przyjmuje parametr `code` i wyświetla stronę z tokenem dostępu.

### POST /api/auth/refresh
Odświeża token dostępu używając refresh token.

## Struktura projektu

- `Controllers/AuthController.cs` - Kontroler obsługujący autoryzację
- `Controllers/CallbackController.cs` - Kontroler obsługujący callback z Spotify
- `Services/AuthService.cs` - Serwis do komunikacji z Spotify API
- `Models/SpotifyModels.cs` - Modele danych dla odpowiedzi Spotify
- `wwwroot/index.html` - Prosta strona testowa do autoryzacji

## Uwagi

- Aplikacja nasłuchuje na porcie 8888
- Redirect URI musi być dokładnie taki sam jak w konfiguracji Spotify
- Tokeny dostępu wygasają po godzinie, użyj refresh token do ich odświeżenia
- **Używaj adresu `127.0.0.1` zamiast `localhost`** - Spotify wymaga adresów loopback dla lokalnego rozwoju

## Environment (.env)

Utwórz plik `.env` bazując na `.env.example` i ustaw zmienne:

```
# Provider: SqlServer lub MySql
DB_PROVIDER=SqlServer

# Connection string do bazy (WebApi)
ConnectionStrings__DbConnectionString=Server=(localdb)\\mssqllocaldb;Database=SpotifyBasicApi2025Db;Trusted_Connection=True;

# Frontend -> adres API
FRONTEND__API_BASE=http://127.0.0.1:8889/
```

## Rozwiązywanie problemów

### Problem z URI przekierowania
Jeśli otrzymujesz błąd "Invalid redirect URI", upewnij się że:
1. W Spotify Developer Dashboard używasz `http://127.0.0.1:8888/callback`
2. W `appsettings.json` masz ustawione `"RedirectUri": "http://127.0.0.1:8888/callback"`
3. Aplikacja nasłuchuje na porcie 8888

### Migracja z localhost
Jeśli wcześniej używałeś `localhost`, musisz zaktualizować:
- Konfigurację w Spotify Developer Dashboard
- Plik `appsettings.json`
- Wszystkie odwołania w kodzie (jeśli występują)

