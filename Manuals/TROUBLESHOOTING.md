# Rozwiązywanie problemów z Spotify API

## Problem: Endpoint `/me/following` zwraca "forbidden"

### Przyczyna
Endpoint `me/following` wymaga scope `user-follow-read`, który nie był uwzględniony w oryginalnej konfiguracji.

### Rozwiązanie

#### 1. Zaktualizuj scope w aplikacji
Scope został już zaktualizowany w `AuthService.cs`:
```csharp
var scope = "user-read-private user-read-email user-follow-read user-library-read playlist-read-private playlist-read-collaborative";
```

#### 2. Ponownie zaloguj się do Spotify
Ponieważ scope się zmienił, musisz ponownie przejść przez proces autoryzacji:

1. Usuń token z pamięci (jeśli używasz)
2. Przejdź do: `https://localhost:xxxx/api/auth/login`
3. Zaloguj się ponownie do Spotify
4. Spotify poprosi o nowe uprawnienia

#### 3. Sprawdź token w Spotify Dashboard
1. Przejdź do [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Wybierz swoją aplikację
3. Sprawdź czy redirect URI jest poprawny
4. Upewnij się, że aplikacja ma odpowiednie uprawnienia

### Testowanie

#### Test 1: Sprawdź profil użytkownika
```http
GET https://localhost:xxxx/api/spotify/profile
```

#### Test 2: Sprawdź zapisane utwory
```http
GET https://localhost:xxxx/api/spotify/tracks/saved
```

#### Test 3: Sprawdź śledzonych artystów
```http
GET https://localhost:xxxx/api/spotify/artists/followed
```

### Dodatkowe scope'y

Oto lista przydatnych scope'ów dla różnych endpointów:

| Endpoint | Wymagany scope |
|----------|----------------|
| `/me` | `user-read-private` |
| `/me/following` | `user-follow-read` |
| `/me/tracks` | `user-library-read` |
| `/me/albums` | `user-library-read` |
| `/me/playlists` | `playlist-read-private` |
| `/me/top/tracks` | `user-top-read` |
| `/me/player` | `user-read-playback-state` |

### Debugowanie

#### 1. Sprawdź logi aplikacji
Aplikacja teraz loguje błędy Spotify API w konsoli.

#### 2. Sprawdź token
```http
GET https://localhost:xxxx/api/auth/token
```

#### 3. Sprawdź odpowiedź Spotify
Jeśli nadal masz problemy, sprawdź dokładną odpowiedź Spotify w logach aplikacji.

### Częste problemy

#### Problem: "Invalid scope"
- Sprawdź czy scope jest poprawnie sformatowany
- Upewnij się, że nie ma duplikatów

#### Problem: "Invalid redirect URI"
- Sprawdź czy redirect URI w kodzie i w Spotify Dashboard są identyczne
- Upewnij się, że używasz HTTPS w produkcji

#### Problem: "Token expired"
- Token automatycznie się odświeża
- Jeśli problem się powtarza, zaloguj się ponownie

### Przydatne linki

- [Spotify Web API Scopes](https://developer.spotify.com/documentation/web-api/concepts/scopes)
- [Spotify Web API Endpoints](https://developer.spotify.com/documentation/web-api/reference)
- [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
