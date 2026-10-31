# Release 3.9.0

Porównanie zmian: `3.8.0` → `3.9.0`.

## Najważniejsze zmiany

### Odtwarzacz: utwory i wykonawcy

- Dodano obejście problemu klientów Spotify, w którym żądanie odtwarzania z listą `uris` zwraca `204`, ale wybrane utwory nie zaczynają grać.
- Backend najpierw sprawdza standardowe odtwarzanie. Jeżeli nie ruszy wybrany utwór, uruchamia go przez kontekst jego albumu, a pozostałe utwory dodaje kolejno do kolejki na tym samym urządzeniu.
- Poprawka obejmuje pojedyncze utwory, odtwarzanie od wskazanego miejsca na liście **/tracks**, przefiltrowane wyniki oraz top utwory wykonawców i podobnych wykonawców.
- Obsługiwane są również zamienne identyfikatory utworów zwracane przez Spotify dla danego rynku.
- Interfejs pokazuje komunikat o użyciu obejścia oraz informację, gdy do kolejki udało się dodać tylko część utworów.
- Obejście ma ograniczenia: po końcu listy Spotify może kontynuować album, a wcześniejsza ręczna kolejka może nadal być obecna.

### Playlisty i początek odtwarzania

- Całe playlisty są uruchamiane przez `context_uri`, bez pobierania do odtwarzania tylko pierwszych 500 utworów z lokalnej biblioteki.
- Start albumu lub playlisty jawnie wskazuje pierwszy utwór i czas `0:00`, jeżeli żądanie nie określa innej pozycji.
- Zwykłe wznowienie odtwarzania pozostaje bez zmiany pozycji; jawnie podany utwór i czas są zachowywane.

### Urządzenia i komunikaty

- Polecenie odtwarzania wskazuje konkretne urządzenie, preferuje aktywne i pomija urządzenia z ograniczonym sterowaniem.
- Nieaktywne urządzenie jest aktywowane przed rozpoczęciem odtwarzania, a backend czeka na potwierdzenie jego gotowości.
- Stan odtwarzacza jest sprawdzany po poleceniu; sam status `204` nie powoduje już oznaczenia nieudanej próby jako sukcesu.
- Widoki korzystają ze wspólnej obsługi błędów i ostrzeżeń, bez niezależnego przenoszenia odtwarzania na pierwsze urządzenie po błędzie.

## Zmiany techniczne i weryfikacja

- `PUT /api/player/play` nadal zwraca `204` przy zwykłym sukcesie. Przy użyciu obejścia lub częściowym dodaniu kolejki zwraca `200` z polem `warning`; brak potwierdzenia odtwarzania jest zgłaszany jako błąd.
- Nie są wymagane migracje bazy danych. Wdrożenie wymaga aktualizacji backendu i frontendu.
- Weryfikacja: `dotnet test Tests/TagDateSorting.Tests/TagDateSorting.Tests.csproj --no-restore --artifacts-path artifacts/playback-check --verbosity quiet` — 39 testów zaliczonych, 1 pominięty (integracja MySQL).
- Testy obejmują m.in. zignorowane `204`, aktywację urządzenia, odtwarzanie przez album, kolejność dodawania do kolejki, zamienne identyfikatory utworów, częściowy błąd kolejki i komunikaty interfejsu.
- Działanie na rzeczywistym urządzeniu Spotify wymaga potwierdzenia po wdrożeniu.
