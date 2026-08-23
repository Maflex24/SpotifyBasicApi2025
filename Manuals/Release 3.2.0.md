# Release 3.2.0

Porównanie zmian: `3.1.0` → `3.2.0`.

## Najważniejsze zmiany

### Konta systemowe i administracja

- Dodano niezależne konta systemowe `AppUser`. Konto Spotify nadal jest opcjonalne i służy wyłącznie do biblioteki oraz dostępu do AI.
- Pierwsze konto administracyjne tworzy się jednorazowo na stronie **/setup**. Nie ma publicznej rejestracji.
- Administrator może zarządzać kontami systemowymi, resetować hasła, nadawać uprawnienia administratora oraz konfigurować AI.
- Górna belka pokazuje niezależnie stan Spotify i stan konta systemowego. Poprawiono także odstęp między tymi statusami.
- Hasła są przechowywane jako hashe PBKDF2, a sesja systemowa korzysta z losowego tokenu w ciasteczku `HttpOnly`.

### Konfiguracja asystenta AI

- Panel administratora **AI** pozwala włączyć lub wyłączyć asystenta, ustawić klucze OpenAI i Last.fm, model domyślny, dostęp dla wszystkich użytkowników Spotify albo allowlisty oraz limit dzienny.
- Klucze API są szyfrowane przez ASP.NET Data Protection i nigdy nie wracają w odpowiedzi API. Puste pole zachowuje zapisany klucz, a jego usunięcie wymaga jawnej akcji.
- Dostęp do listy modeli jest pobierany dla skonfigurowanego klucza OpenAI.
- Historyczne zmienne środowiskowe AI nie są już źródłem konfiguracji; ustawienia należy wprowadzić w panelu administratora.

### Kontekstowy czat w bibliotece

- Samodzielny ekran asystenta został zastąpiony wysuwanym panelem czatu po prawej stronie.
- Przyciski **AI** są dostępne przy albumach, artystach i utworach na listach oraz stronach szczegółów. Nazwy elementów nadal zachowują swoje dotychczasowe działanie nawigacyjne.
- Panel zachowuje bieżący wątek w sesji przeglądarki, obsługuje krótką historię rozmowy, szybkie pytania oraz przypisywanie propozycji istniejących tagów.
- Dostęp AI jest sprawdzany globalnie raz na sesję aplikacji. Usunięto kosztowne inicjalizowanie sprawdzenia dostępu dla każdego kafelka listy.
- Zoptymalizowano przywracanie lokalnie wylosowanego albumu: aplikacja nie doczytuje automatycznie kolejnych stron wyników, a fokus pojawia się dopiero, gdy użytkownik ręcznie załaduje stronę zawierającą ten album.

### Rzetelniejsze informacje muzyczne

- AI obsługuje pytania po polsku o gatunki, podobnych wykonawców i albumy, popularne albumy, tagi, historię zespołu, datę powstania, skład oraz opis albumu.
- MusicBrainz dostarcza gatunki, metadane wydań, daty działalności i relacje członkowskie. Wikidata uzupełnia dane encyklopedyczne, a Last.fm dostarcza popularność oraz podobieństwo.
- Odpowiedzi zawierają strukturalne fakty i klikane linki prowadzące do konkretnych stron artysty, albumu lub encji źródłowej.
- Model otrzymuje wyłącznie pakiet danych zewnętrznych dla aktualnego kontekstu. Nie otrzymuje całej prywatnej biblioteki i ma sygnalizować brak danych zamiast je uzupełniać.
- Last.fm rozróżnia brak klucza, błąd API i brak wyników. Źródło nie jest pokazywane, jeśli nie zwróciło danych; błędne odpowiedzi nie trafiają do cache.

### AI w trybie Blind

- W widoku **/albums** przyciski AI są dostępne także w trybie Blind dla ukrytych albumów i artystów.
- Panel, szybkie pytania i zwykłe odpowiedzi nie pokazują nazwy albumu ani wykonawcy. Nazwy nie są przekazywane do modelu w tym trybie.
- Informacje identyfikujące, m.in. skład zespołu i opis encyklopedyczny, są ukrywane w standardowej odpowiedzi Blind.
- Użytkownik może świadomie ujawnić tożsamość, pytając wprost np. „jaki to album?” lub „kto to jest?”. Odpowiedź jest wtedy generowana lokalnie, bez użycia modelu i bez kosztu tokenów.
- Wątki Blind nie są utrwalane w `sessionStorage`, a przełączenie trybu Blind czyści otwarty panel. Linki do konkretnych źródeł pozostają dostępne jako świadoma droga do ujawnienia informacji poza aplikacją.

## Zmiany techniczne i wdrożenie

- Dodano migracje EF Core dla ustawień AI, cache źródeł, metryk użycia, kont `AppUser` i ich sesji. Przed wdrożeniem należy zastosować migracje bazy danych.
- Docker powinien zachowywać trwały wolumin kluczy Data Protection; jego utrata unieważnia możliwość odszyfrowania zapisanych sekretów i sesji.
- Po wdrożeniu administrator tworzy pierwsze konto na **/setup**, loguje się do konta systemowego i wprowadza klucze AI w panelu **AI**.
- Brak projektu testowego w repozytorium nie zmienia sposobu użycia aplikacji; rekomendowana weryfikacja wydania to kompilacja rozwiązania oraz ręczne sprawdzenie logowania, konfiguracji AI, czatu i trybu Blind.
