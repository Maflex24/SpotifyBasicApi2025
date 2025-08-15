# System Tagowania Biblioteki

## 📋 **Przegląd**

System tagowania pozwala użytkownikom na dodawanie własnych tagów do elementów Spotify (artystów, albumów, utworów, playlist). Każdy użytkownik ma własną bibliotekę tagów, która jest powiązana z jego kontem Spotify.

## 🏗️ **Struktura Encji**

### **LibTagGroup** - Grupy tagów
```csharp
public class LibTagGroup
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ForAlbum { get; set; }
    public bool ForArtist { get; set; }
    public bool ForTrack { get; set; }
    public bool ForPlaylist { get; set; }
    public string UserId { get; set; }
    public virtual ICollection<LibTag> Tags { get; set; }
}
```

### **LibTag** - Pojedyncze tagi
```csharp
public class LibTag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool ForAlbum { get; set; }
    public bool ForArtist { get; set; }
    public bool ForTrack { get; set; }
    public bool ForPlaylist { get; set; }
    public long TagGroupId { get; set; }
    public string UserId { get; set; }
    public virtual LibTagGroup TagGroup { get; set; }
}
```

### **LibTagAssign** - Powiązania tagów z elementami
```csharp
public class LibTagAssign
{
    public long Id { get; set; }
    public long TagId { get; set; }
    public string UserId { get; set; }
    public float Strength { get; set; } = 3.0f; // Siła tagu (0.0-6.0)
    public string ItemType { get; set; } // "artist", "album", "track", "playlist"
    public string SpotifyItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Relacje
    public virtual LibTag Tag { get; set; }
    public virtual SpUser User { get; set; }
    public virtual SpArtist? Artist { get; set; }
    public virtual SpAlbum? Album { get; set; }
    public virtual SpTrack? Track { get; set; }
    public virtual SpPlaylist? Playlist { get; set; }
}
```

## 🔗 **Relacje**

```
SpUser (maflex24)
├── LibTagGroup ("Ulubione gatunki")
│   ├── LibTag ("Rock")
│   ├── LibTag ("Jazz")
│   └── LibTag ("Electronic")
└── LibTagAssign
    ├── Tag: "Rock" → Artist: "Queen" (Strength: 5.0)
    ├── Tag: "Jazz" → Album: "Kind of Blue" (Strength: 4.0)
    └── Tag: "Electronic" → Track: "Strobe" (Strength: 3.0)
```

## 🌐 **API Endpoints**

### **Grupy Tagów**

#### `GET /api/library/tag-groups`
Pobiera wszystkie grupy tagów użytkownika.

#### `GET /api/library/tag-groups/{id}`
Pobiera konkretną grupę tagów.

#### `POST /api/library/tag-groups`
Tworzy nową grupę tagów.
```json
{
  "name": "Ulubione gatunki",
  "forAlbum": true,
  "forArtist": true,
  "forTrack": false,
  "forPlaylist": false
}
```

#### `PUT /api/library/tag-groups/{id}`
Aktualizuje grupę tagów.

#### `DELETE /api/library/tag-groups/{id}`
Usuwa grupę tagów (usuwa również wszystkie tagi w grupie).

### **Tagi**

#### `GET /api/library/tags`
Pobiera wszystkie tagi użytkownika.

#### `GET /api/library/tags/{id}`
Pobiera konkretny tag.

#### `POST /api/library/tags`
Tworzy nowy tag.
```json
{
  "name": "Rock",
  "forAlbum": true,
  "forArtist": true,
  "forTrack": false,
  "forPlaylist": false,
  "tagGroupId": 1
}
```

#### `PUT /api/library/tags/{id}`
Aktualizuje tag.

#### `DELETE /api/library/tags/{id}`
Usuwa tag (usuwa również wszystkie powiązania).

### **Tagowane Elementy**

#### `POST /api/library/tagged-items`
Dodaje tag do elementu.
```json
{
  "tagId": 1,
  "spotifyItemId": "4iJyoBOLtHqaGxP12qzhQI",
  "itemType": "artist"
}
```

#### `DELETE /api/library/tagged-items`
Usuwa tag z elementu.
```json
{
  "tagId": 1,
  "spotifyItemId": "4iJyoBOLtHqaGxP12qzhQI",
  "itemType": "artist"
}
```

#### `GET /api/library/tagged-items/{itemType}/{spotifyItemId}`
Pobiera wszystkie tagi przypisane do konkretnego elementu.

#### `GET /api/library/tagged-items/by-tag/{tagId}`
Pobiera wszystkie elementy z konkretnym tagiem.

#### `GET /api/library/tagged-items/by-type/{itemType}`
Pobiera wszystkie tagowane elementy danego typu.

**Uwaga**: Wszystkie relacje używają `ON DELETE NO ACTION` aby uniknąć cykli kaskadowych w SQL Server.

### **Statystyki**

#### `GET /api/library/statistics`
Pobiera statystyki tagowania użytkownika.
```json
{
  "tagStatistics": {
    "artist": 15,
    "album": 8,
    "track": 3,
    "playlist": 2
  },
  "taggedItemsCount": {
    "artists": 12,
    "albums": 6,
    "tracks": 2,
    "playlists": 1
  }
}
```

## 💡 **Przykłady Użycia**

### **1. Tworzenie grupy tagów**
```bash
curl -X POST http://localhost:8888/api/library/tag-groups \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nastroje",
    "forAlbum": true,
    "forArtist": false,
    "forTrack": true,
    "forPlaylist": true
  }'
```

### **2. Dodawanie tagu do artysty**
```bash
curl -X POST http://localhost:8888/api/library/tagged-items \
  -H "Content-Type: application/json" \
  -d '{
    "tagId": 1,
    "spotifyItemId": "4iJyoBOLtHqaGxP12qzhQI",
    "itemType": "artist"
  }'
```

### **3. Pobieranie tagów artysty**
```bash
curl http://localhost:8888/api/library/tagged-items/artist/4iJyoBOLtHqaGxP12qzhQI
```

## 🔧 **Typy Elementów**

Dostępne typy elementów (enum `SpotifyItemType`):
- `"artist"` - Artyści
- `"album"` - Albumy
- `"track"` - Utwory
- `"playlist"` - Playlisty

## 🚀 **Następne Kroki**

1. **Utworzenie migracji**:
   ```bash
   dotnet ef migrations add AddLibraryEntities
   dotnet ef database update
   ```

2. **Testowanie API**:
   - Utwórz grupę tagów
   - Dodaj kilka tagów
   - Otaguj elementy Spotify
   - Sprawdź statystyki

3. **Integracja z frontendem**:
   - Interfejs do zarządzania tagami
   - Dodawanie tagów do elementów
   - Filtrowanie po tagach

## 📝 **Uwagi**

- Wszystkie operacje są powiązane z użytkownikiem Spotify (obecnie hardcoded "maflex24")
- Tagi są unikalne per użytkownik i element
- Usunięcie grupy tagów usuwa wszystkie tagi w grupie
- Usunięcie tagu usuwa wszystkie powiązania z elementami
