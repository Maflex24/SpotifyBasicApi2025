# Asystent AI i konta systemowe

## Cel

Aplikacja pomaga odkrywać, opisywać i organizować bibliotekę Spotify. Czat AI korzysta z metadanych MusicBrainz oraz Last.fm, a odpowiedzi pokazują źródła, metryki i wymagające zatwierdzenia propozycje tagów.

## Źródła i granice

- Spotify identyfikuje elementy biblioteki oraz obsługuje opcjonalne logowanie do niej.
- Do MVP nie używamy Spotify Related Artists ani Recommendations, Reddita, Discogs ani Metal Archives.
- MusicBrainz dostarcza metadane i gatunki; Last.fm podobieństwo oraz popularność. Popularność nie jest przedstawiana jako ocena.
- Treść prywatnej biblioteki Spotify nie jest wysyłana do modelu AI.

## Konta i uprawnienia

- `AppUser` jest niezależnym kontem systemowym. Zawiera unikalny nick, hash hasła PBKDF2 tworzony przez `PasswordHasher<AppUser>` oraz flagę `IsAdmin`.
- Gość może korzystać z biblioteki. Spotify i konto systemowe są dwoma niezależnymi stanami logowania.
- Pierwszy administrator jest zakładany jednokrotnie na stronie `/setup`, wyłącznie gdy nie ma żadnego `AppUser`.
- Nie ma publicznej rejestracji. Administrator zarządza kontami; nie można usunąć ani zdegradować ostatniego administratora.
- Tylko aktywny administrator systemowy ma dostęp do administracji i ustawień AI. Dostęp do czatu AI nadal określa zalogowanie Spotify oraz tryb wszyscy/allowlista identyfikatorów Spotify.

## Sesje i sekrety

- Sesja `AppUser` jest losowym tokenem w osobnym ciasteczku `HttpOnly`, `Secure`, `SameSite=Lax`; w bazie znajduje się wyłącznie hash tokenu. Domyślna ważność: 30 dni.
- OpenAI i Last.fm API keys przechowujemy zaszyfrowane w `AiSettings` przez ASP.NET Data Protection. API nigdy nie zwraca ich wartości.
- Pusty klucz przy zapisie zachowuje poprzednią wartość; usunięcie wymaga jawnej akcji.
- Wolumin kluczy Data Protection w Dockerze jest trwały, aby restart nie unieważniał sekretów ani sesji.

## Konfiguracja AI

- Panel administratora ustawia włączenie AI, klucze OpenAI i Last.fm, model domyślny, tryb dostępu Spotify, allowlistę, limit dzienny i zużycie.
- Lista modeli jest pobierana dla zapisanej konfiguracji i ograniczona do modeli tekstowych zgodnych z aplikacją.
- Historyczne zmienne `AI_ADMIN_SPOTIFY_USER_IDS`, `OPENAI_API_KEY`, `LASTFM_API_KEY` i `AI_ALLOWED_MODEL_IDS` nie są źródłem konfiguracji po migracji.

## Dane, cache i niezawodność

- Cache źródeł, limity żądań oraz deterministyczne rankingi ograniczają koszt i poprawiają powtarzalność.
- Historia rozmów pozostaje tylko w bieżącej sesji przeglądarki; backend zapisuje wyłącznie metryki użycia.
- Brak klucza, modelu, danych źródłowych lub awaria dostawcy zwracają czytelny stan bez wycieku sekretów.

## Weryfikacja

- Testujemy setup, login/logout, sesję wygasłą, błędne hasło i ochronę ostatniego administratora.
- Testujemy izolację panelu dla gościa i zwykłego użytkownika oraz niezależne statusy Spotify/AppUser.
- Testujemy szyfrowanie sekretów, brak ich w API/logach, zachowanie pustego pola, jawne usunięcie, cache, limity i błędy dostawców.
