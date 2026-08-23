# Asystent AI i konta systemowe

## Cel

Aplikacja pomaga odkrywać, opisywać i organizować bibliotekę Spotify. Czat AI korzysta z metadanych MusicBrainz, Wikidata i Last.fm, a odpowiedzi pokazują źródła, metryki, fakty oraz wymagające zatwierdzenia propozycje tagów.

## Źródła i granice

- Spotify identyfikuje elementy biblioteki oraz obsługuje opcjonalne logowanie do niej.
- Do MVP nie używamy Spotify Related Artists ani Recommendations, Reddita, Discogs ani Metal Archives.
- MusicBrainz dostarcza metadane, gatunki, daty działalności, dane wydań oraz relacje członkowskie. Wikidata uzupełnia encyklopedyczne fakty przez stabilne identyfikatory MusicBrainz. Last.fm dostarcza podobieństwo oraz popularność; popularność nie jest przedstawiana jako ocena.
- Odpowiedzi informacyjne, np. „opowiedz mi o tym zespole”, są syntezą wyłącznie faktów z pakietu badawczego. Model nie uzupełnia ich własną wiedzą i jasno sygnalizuje brak danych lub niejednoznaczne dopasowanie.
- Tematyka i pełne teksty utworów nie należą do obecnego zakresu; wymagają późniejszego wyboru legalnego źródła.
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
- Cache przechowuje wyłącznie poprawne odpowiedzi źródeł. Dane stabilne MusicBrainz/Wikidata są cache’owane dłużej niż dane Last.fm; błędne odpowiedzi Last.fm nie są cache’owane ani prezentowane jako źródło.
- Czat jest panelem kontekstowym otwieranym z przycisków `AI` przy albumach, artystach i utworach, a nie osobnym ekranem roboczym. Jego nagłówek zawsze wskazuje wybrany element.
- W trybie Blind na `/albums` przyciski AI są dostępne dla ukrytych albumów i artystów, lecz panel, zapisany wątek i zwykłe odpowiedzi nie pokazują ich nazw. Do modelu trafia wyłącznie anonimowy kontekst oraz zredagowane fakty; bezpośrednia prośba o nazwę świadomie ujawnia ją bez wywołania modelu.
- Konkretne linki źródłowe pozostają w Blind widoczne jako świadoma droga do ujawnienia informacji poza aplikacją.
- Aktywny wątek pozostaje tylko w `sessionStorage` bieżącej sesji przeglądarki. Do modelu trafia bieżące pytanie, typ i nazwa aktualnie wybranego elementu oraz najwyżej sześć poprzednich wiadomości; backend zapisuje wyłącznie metryki użycia. Wątki Blind nie są utrwalane, aby ujawniona na żądanie nazwa nie wróciła po odświeżeniu.
- Playlisty nie są kontekstem AI. W widoku playlisty można otwierać czat dla jej utworów, albumów i artystów bez przekazywania modelowi składu playlisty.
- Brak klucza, modelu, danych źródłowych lub awaria dostawcy zwracają czytelny stan bez wycieku sekretów.
- Źródła pod odpowiedzią prowadzą do konkretnych stron encji (artysty, release group lub elementu Wikidata), nigdy wyłącznie do strony głównej dostawcy.

## Weryfikacja

- Testujemy setup, login/logout, sesję wygasłą, błędne hasło i ochronę ostatniego administratora.
- Testujemy izolację panelu dla gościa i zwykłego użytkownika oraz niezależne statusy Spotify/AppUser.
- Testujemy szyfrowanie sekretów, brak ich w API/logach, zachowanie pustego pola, jawne usunięcie, cache, limity i błędy dostawców.
- Testujemy polskie pytania o profil, historię, skład i wydanie, brak halucynacji przy niejednoznacznym dopasowaniu oraz linki do konkretnych encji źródłowych.
