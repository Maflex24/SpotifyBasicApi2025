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
DB_PROVIDER=MySql

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

## Wdrożenie na VPS (z centralnym nginx)

Zakładamy, że działa centralny reverse proxy (repo `OVH-cloud---VPS-Serwer---nginx`) i istnieje sieć `edge-proxy`. Domeny: `gspotifylibrarymanager.com`, `www.gspotifylibrarymanager.com`.

1) Przygotuj `.env` (na bazie `env.example.prod`):
```
FRONTEND_URL=https://gspotifylibrarymanager.com
ConnectionStrings__DbConnectionString=Server=db;Port=3306;Database=spotifybasic;User=root;Password=<silne_haslo>;SslMode=None;
MYSQL_ROOT_PASSWORD=<silne_haslo>
MYSQL_DATABASE=spotifybasic
SPOTIFY_CLIENT_ID=<from Spotify>
SPOTIFY_CLIENT_SECRET=<from Spotify>
SPOTIFY_REDIRECT_URI=https://gspotifylibrarymanager.com/callback
SPOTIFY_API_URL=https://api.spotify.com/v1/
```

2) Upewnij się, że sieć istnieje:
```bash
docker network create edge-proxy || true
```

3) Zbuduj i uruchom:
```bash
-- docker compose --env-file ./env.example.prod up -d --build
docker compose --env-file ./.env up -d --build
```

4) Wydaj certyfikaty w centralnym proxy (jeśli nie zrobione) i przeładuj nginx:
```bash
cd ../OVH-cloud---VPS-Serwer---nginx
docker compose run --rm certbot certonly --webroot -w /var/www/certbot \
  -d gspotifylibrarymanager.com -d www.gspotifylibrarymanager.com
docker compose exec nginx nginx -s reload
```

5) Przydatne komendy:
```bash
docker compose up -d --build
docker compose down
docker compose logs -f webapi
docker compose logs -f frontend
```

Uwagi:
- WebApi czeka 25s po starcie DB (wpis `entrypoint` z `sleep 25`).
- Ruch do frontu i API idzie przez `edge-proxy` (aliasy: `spotify-frontend`, `spotify-webapi`).

### Kopia bazy danych (produkcja: Debian + Docker)

Poniżej szybkie polecenia do wykonania kopii bazy MySQL działającej w kontenerze Dockera na VPS (Debian). Dane pochodzą z `docker-compose.yml`:
- kontener bazy: `spotifybasic-db`
- zmienne w kontenerze: `MYSQL_ROOT_PASSWORD`, `MYSQL_DATABASE`

1) Utwórz katalog na kopie i przejdź do katalogu projektu:
```bash
mkdir -p ./backups/mysql
```

2) Wykonaj dump z poziomu hosta (kompresja gzip, spójny snapshot dla InnoDB):
```bash
docker exec -i spotifybasic-db sh -c 'mysqldump --single-transaction --quick --routines --triggers --events -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE"' \
  | gzip > ./backups/mysql/spotifybasic-$(date +%F_%H%M%S).sql.gz
```

3) (Opcjonalnie) Dump bez kompresji:
```bash
docker exec -i spotifybasic-db sh -c 'mysqldump --single-transaction --quick --routines --triggers --events -u root -p"$MYSQL_ROOT_PASSWORD" "$MYSQL_DATABASE"' \
  > ./backups/mysql/spotifybasic-$(date +%F_%H%M%S).sql
```

4) Szybka weryfikacja kopii (zobacz kilka pierwszych linii):
```bash
zcat ./backups/mysql/spotifybasic-*.sql.gz | head -n 20
```

5) Pobranie kopii z VPS na lokalny komputer (z poziomu Twojego komputera):
```bash
scp <uzytkownik>@<twoj-vps>:/sciezka/do/projektu/backups/mysql/spotifybasic-YYYY-MM-DD_HHMMSS.sql.gz .
```

Uwagi:
- Polecenia zakładają, że pracujesz w katalogu projektu (obok pliku `docker-compose.yml`).
- Jeśli nie pamiętasz nazwy kontenera, sprawdź: `docker ps --format '{{.Names}}' | grep spotifybasic-db`.
- `--single-transaction` zapewnia spójny zrzut dla tabel InnoDB bez blokad zapisu.

