# Spotify Artists API - Dokumentacja

## Przegląd

Zaimplementowano funkcjonalność pobierania śledzonych artystów z Spotify API z obsługą paginacji i zapisywania w bazie danych.

## Endpointy

### 1. Pobieranie śledzonych artystów (z paginacją)

```http
GET /api/spotify/artists/followed?limit=50
```

**Parametry:**
- `limit` (opcjonalny): Liczba artystów do pobrania (1-50, domyślnie 50)

**Odpowiedź:**
```json
{
  "artists": {
    "href": "https://api.spotify.com/v1/me/following?type=artist&limit=50",
    "limit": 50,
    "next": "https://api.spotify.com/v1/me/following?type=artist&limit=50&after=0I2XqVXqHScXjHhk6AYYRe",
    "cursors": {
      "after": "0I2XqVXqHScXjHhk6AYYRe",
      "before": null
    },
    "total": 150,
    "items": [
      {
        "external_urls": {
          "spotify": "https://open.spotify.com/artist/..."
        },
        "followers": {
          "href": null,
          "total": 1000000
        },
        "genres": ["rock", "alternative"],
        "href": "https://api.spotify.com/v1/artists/...",
        "id": "0I2XqVXqHScXjHhk6AYYRe",
        "images": [
          {
            "url": "https://i.scdn.co/image/...",
            "height": 300,
            "width": 300
          }
        ],
        "name": "Nazwa Artysty",
        "popularity": 85,
        "type": "artist",
        "uri": "spotify:artist:0I2XqVXqHScXjHhk6AYYRe"
      }
    ]
  }
}
```

### 2. Pobieranie wszystkich śledzonych artystów (automatyczna paginacja)

```http
GET /api/spotify/artists/followed/all
```

**Odpowiedź:**
```json
{
  "total": 150,
  "artists": [
    {
      "id": "0I2XqVXqHScXjHhk6AYYRe",
      "name": "Nazwa Artysty",
      "genres": ["rock", "alternative"],
      "popularity": 85,
      "images": [...],
      ...
    }
  ]
}
```

### 3. Synchronizacja z bazą danych

```http
POST /api/spotify/artists/sync
```

**Odpowiedź:**
```json
{
  "message": "Zapisano 150 artystów w bazie danych",
  "totalArtists": 150,
  "savedCount": 150
}
```

## Modele danych

### SpotifyFollowedArtistsResponse
Główny model odpowiedzi z endpointu `/me/following`

### SpotifyArtistObject
Model reprezentujący pojedynczego artystę z Spotify API

### SpArtist
Model encji w bazie danych (z przedrostkiem "Sp")

## Funkcjonalności

### 1. Automatyczna paginacja
- Maksymalny limit: 50 artystów na żądanie
- Automatyczne pobieranie wszystkich stron
- Użycie cursor `after` do nawigacji

### 2. Mapowanie danych
- Konwersja z Spotify API na modele C#
- Obsługa list (gatunki, obrazy) jako stringi z separatorem
- Zachowanie wszystkich właściwości artysty

### 3. Synchronizacja z bazą danych
- Sprawdzanie czy artysta już istnieje
- Aktualizacja istniejących rekordów
- Dodawanie nowych artystów
- Licznik zapisanych rekordów

### 4. Zapytania do bazy danych
- Pobieranie wszystkich artystów
- Filtrowanie po gatunku
- Sortowanie po popularności
- Top artystów

## Przykład użycia

### 1. Pobranie pierwszej strony artystów
```csharp
var response = await spotifyApiService.GetFollowedArtists(20);
var artists = response?.Artists?.Items ?? new List<SpotifyArtistObject>();
```

### 2. Pobranie wszystkich artystów
```csharp
var allArtists = await spotifyApiService.GetAllFollowedArtists();
```

### 3. Zapisanie w bazie danych
```csharp
var savedCount = await spotifyDataService.SaveFollowedArtistsAsync(allArtists);
```

### 4. Pobranie z bazy danych
```csharp
var topArtists = await spotifyDataService.GetTopArtistsAsync(10);
var rockArtists = await spotifyDataService.GetArtistsByGenreAsync("rock");
```

## Obsługa błędów

- **401 Unauthorized**: Token wygasł lub nieważny
- **403 Forbidden**: Brak uprawnień (sprawdź scope `user-follow-read`)
- **429 Too Many Requests**: Przekroczono limit zapytań
- **500 Internal Server Error**: Błąd serwera

## Scope'y wymagane

- `user-follow-read` - dostęp do śledzonych artystów

## Ograniczenia API

- Maksymalny limit: 50 artystów na żądanie
- Rate limiting: 100 zapytań na minutę
- Paginacja: użycie cursor `after` dla następnych stron
