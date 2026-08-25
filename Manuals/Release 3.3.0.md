# Release 3.3.0

Porównanie zmian: `3.2.0` → `3.3.0`.

## Najważniejsze zmiany

### Licznik pozycji utworu w playerze

- Standardowy player pokazuje teraz po tytule bieżącego utworu jego pozycję na albumie w formacie **`Utwór X/Y`**, np. `Nazwa utworu · Utwór 3/8`.
- Licznik korzysta z numeru utworu i całkowitej liczby utworów zwróconych przez Spotify. Jeśli którejkolwiek wartości brakuje, etykieta nie jest wyświetlana i player działa jak wcześniej.
- Ten sam sposób wyznaczania pozycji jest wykorzystywany w standardowym widoku oraz w trybie Blind.

### Albumy: dopasowania tagów i nawigacja

- Przy włączonej opcji **Wyświetl i szukaj tagów artystów** karty albumów mają delikatną niebieską ramkę, jeśli aktywne pozytywne kryteria tagowe spełnia bezpośrednio sam album.
- Ramka pojawia się również wtedy, gdy album równocześnie trafia do wyników przez tag wykonawcy. Nie jest wyświetlana dla albumu pasującego wyłącznie przez tag wykonawcy, w trybie Blind ani bez włączonego wyszukiwania tagów artystów.
- Losowanie z wczytanych wyników nie przewija już automatycznie strony i nie rozwija grupy wykonawcy.
- Obok **Odtwórz wszystko** dodano przyciski **Odtwarzany** i **Wylosowany**. Pozwalają ręcznie przejść odpowiednio do albumu aktualnie odtwarzanego oraz ostatnio wylosowanego, jeżeli jego karta jest już w bieżących wynikach.
- Zapisane filtry albumów poprawnie odtwarzają ustawienie **Wyświetl i szukaj tagów artystów**.

### Wspólne menu kontekstów AI

- Widoczne przyciski tekstowe **AI** zastąpiono przyciskiem **więcej** z trzema kropkami.
- Każde menu zawiera wszystkie konteksty AI dostępne wcześniej przy danym elemencie: album, wykonawców i/lub utwór.
- Menu jest dostępne na listach **/albums**, **/artists**, **/tracks** i utworach playlist oraz na stronach szczegółów artysty i albumu.
- W tabelach utworów przycisk znajduje się przy nazwie utworu, bez dodawania nowej kolumny.
- W trybie Blind menu nie ujawnia nazw ukrytych elementów; przy niedostępnym AI przycisk nie jest renderowany.

## Zmiany techniczne i weryfikacja

- Dodano współdzielony komponent menu AI z listą etykiet i kontekstów czatu, wykorzystywany przez wszystkie widoki biblioteki.
- Nie są wymagane migracje bazy danych ani zmiany kontraktów API.
- Wydanie zweryfikowano komendą `dotnet build Frontend/Frontend.csproj --no-restore`.
