# Release 2.6.0

Porównanie zmian: `2.5.0` → `2.6.0`.

## Najważniejsze zmiany

### Wyszukiwanie artystów poza biblioteką

- Na stronie **Artyści** dodano opcję **Szukaj poza swoją biblioteką**. Przy podanej frazie do wyników mogą zostać dołączone artyści znalezieni bezpośrednio w Spotify.
- Wyniki spoza biblioteki są wyraźnie oznaczone i można je dodać do własnej biblioteki jednym przyciskiem.
- Wyszukiwanie zewnętrzne działa z filtrem gatunku oraz sortowaniem po nazwie lub popularności.
- Opcja jest niedostępna przy aktywnych kryteriach tagów, ponieważ wyniki zewnętrzne nie mają jeszcze lokalnych przypisań tagów.
- Wybrany stan wyszukiwania poza biblioteką jest zachowywany wraz z bieżącymi filtrami oraz zapisanymi filtrami.

### Strona artysty i wydawnictwa

- Widok **Artysta** otrzymał odświeżony nagłówek z tagami oraz przyciskiem dodawania lub usuwania artysty z biblioteki.
- Lista wydawnictw pozwala niezależnie pokazywać **albumy**, **single** i **kompilacje**. Ustawienia tych typów są zapamiętywane lokalnie.
- Każde widoczne wydawnictwo pokazuje stan biblioteki i tagi; z karty można je odtworzyć, dodać do kolejki, dodać albo usunąć z biblioteki oraz otworzyć panel tagowania.
- Dodano akcję **Dodaj wszystkie widoczne**, która dodaje do biblioteki aktualnie wyświetlane wydawnictwa i raportuje ewentualne błędy synchronizacji.

### Synchronizacja biblioteki

- Dodawanie i usuwanie albumów oraz artystów synchronizuje teraz bibliotekę aplikacji z biblioteką Spotify; w razie błędu lokalna zmiana jest wycofywana.
- Backend udostępnia zbiorczy odczyt stanu albumów i artystów w bibliotece, wraz z ich tagami, dzięki czemu widok artysty pokazuje aktualne dane bez osobnych zapytań dla każdej karty.
- Dodano zbiorcze dodawanie albumów do biblioteki, wykorzystywane przez akcję dla wszystkich widocznych wydawnictw.
