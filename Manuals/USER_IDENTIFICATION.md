# Identyfikacja użytkowników w aplikacji

## Przegląd

Aplikacja używa **Spotify User ID** jako unikalnego identyfikatora użytkowników. Nie wymaga to tworzenia własnych kont - użytkownicy logują się przez Spotify OAuth.

## Jak to działa

### 1. Proces autoryzacji

1. **Użytkownik klika "Zaloguj się do Spotify"**
2. **Przekierowanie do Spotify** - użytkownik loguje się na swoje konto Spotify
3. **Callback z kodem** - Spotify przekierowuje z powrotem z kodem autoryzacji
4. **Wymiana kodu na token** - aplikacja wymienia kod na access token
5. **Automatyczne zapisanie użytkownika** - aplikacja pobiera profil użytkownika i zapisuje go w bazie danych

### 2. Unikalny identyfikator

**Spotify User ID** jest unikalnym, niezmiennym identyfikatorem przypisanym do każdego konta Spotify:

```json
{
  "id": "31a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p",
  "display_name": "Jan Kowalski",
  "email": "jan.kowalski@example.com",
  "country": "PL",
  "product": "premium"
}
```

### 3. Automatyczne zapisywanie użytkownika

Po udanej autoryzacji, aplikacja automatycznie:

1. Pobiera profil użytkownika z endpointu `/me`
2. Sprawdza czy użytkownik już istnieje w bazie danych
3. Jeśli istnieje - aktualizuje dane
4. Jeśli nie istnieje - tworzy nowy rekord

## Struktura danych użytkownika

### Tabela `SpUsers`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | string | **Spotify User ID** - klucz główny |
| `DisplayName` | string | Nazwa wyświetlana użytkownika |
| `Email` | string | Email użytkownika (jeśli dostępny) |
| `Country` | string | Kraj użytkownika |
| `Product` | string | Typ subskrypcji (free/premium) |
| `Followers` | int | Liczba obserwujących |
| `Images` | string | Obrazy profilowe (jako string z separatorem) |
| `CreatedAt` | DateTime | Data utworzenia rekordu |
| `UpdatedAt` | DateTime | Data ostatniej aktualizacji |

## Endpointy API

### 1. Pobieranie profilu z Spotify
```http
GET /api/spotify/profile
```

### 2. Pobieranie użytkownika z bazy danych
```http
GET /api/spotify/profile/database
```

### 3. Synchronizacja artystów z użytkownikiem
```http
POST /api/spotify/artists/sync
```

## Przykład użycia

### 1. Pobranie aktualnego użytkownika
```csharp
// Pobierz profil z Spotify
var spotifyProfile = await spotifyApiService.GetCurrentUserProfileAsync();

// Pobierz użytkownika z bazy danych
var dbUser = await spotifyDataService.GetUserByIdAsync(spotifyProfile.Id);

// Użyj Spotify User ID jako identyfikatora
string userId = dbUser.Id; // np. "31a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p"
```

### 2. Tworzenie własnych elementów z powiązaniem do użytkownika
```csharp
// Przykład: własne tagi dla artystów
public class UserArtistTag
{
    public int Id { get; set; }
    public string UserId { get; set; } // Spotify User ID
    public string ArtistId { get; set; } // Spotify Artist ID
    public string TagName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Zalety tego rozwiązania

### ✅ **Brak własnych kont**
- Użytkownicy nie muszą tworzyć nowych kont
- Wykorzystuje istniejące konta Spotify
- Bezpieczne - Spotify zarządza hasłami

### ✅ **Unikalny identyfikator**
- Spotify User ID jest unikalny i niezmienny
- Nie ma ryzyka duplikatów
- Można bezpiecznie używać jako klucza obcego

### ✅ **Automatyczna synchronizacja**
- Dane użytkownika są automatycznie aktualizowane
- Zawsze aktualne informacje z Spotify
- Brak potrzeby ręcznego zarządzania

### ✅ **Rozszerzalność**
- Łatwo dodać własne pola do tabeli `SpUsers`
- Można tworzyć własne tabele powiązane z użytkownikami
- Elastyczna struktura danych

## Przykłady własnych funkcjonalności

### 1. Własne tagi dla artystów
```sql
CREATE TABLE UserArtistTags (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- Spotify User ID
    ArtistId NVARCHAR(450) NOT NULL, -- Spotify Artist ID
    TagName NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES SpUsers(Id),
    FOREIGN KEY (ArtistId) REFERENCES SpArtists(Id)
);
```

### 2. Własne playlisty
```sql
CREATE TABLE UserCustomPlaylists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- Spotify User ID
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES SpUsers(Id)
);
```

### 3. Oceny utworów
```sql
CREATE TABLE UserTrackRatings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- Spotify User ID
    TrackId NVARCHAR(450) NOT NULL, -- Spotify Track ID
    Rating INT NOT NULL, -- 1-5 gwiazdek
    Comment NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES SpUsers(Id),
    FOREIGN KEY (TrackId) REFERENCES SpTracks(Id)
);
```

## Bezpieczeństwo

- **Spotify User ID** jest publicznym identyfikatorem
- Nie zawiera wrażliwych danych
- Można bezpiecznie używać w URL-ach i API
- Spotify zarządza bezpieczeństwem konta

## Ograniczenia

- Użytkownicy muszą mieć konto Spotify
- Zależność od dostępności Spotify API
- Dane użytkownika są ograniczone do tego, co udostępnia Spotify
