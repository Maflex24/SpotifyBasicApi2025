# Release 2.3.0

Porównanie zmian: `2.2.0` → `2.3.0`.

## Najważniejsze zmiany

### Paginacja albumów

- Widok **Albumy** pobiera wyniki porcjami po maksymalnie 100 pozycji. Kolejna strona jest automatycznie doczytywana po dojściu do dołu listy.
- Nagłówek pokazuje postęp w formacie **Albumy X/Y albumów**, gdzie X oznacza liczbę aktualnie wczytanych kart, a Y pełną liczbę albumów spełniających zastosowane kryteria.
- Infinite scroll korzysta wyłącznie z zastosowanego snapshotu filtrów. Zmiana filtrów, sortowania, wyszukiwania lub wczytanie zapisanego filtra rozpoczyna listę ponownie od pierwszej strony.
- **Losuj** i **Odtwórz wszystko** działają na aktualnie wczytanych albumach.

### Wyszukiwanie albumów

- Wyszukiwanie na liście albumów jest wykonywane przez backend, a nie lokalnie w przeglądarce.
- Fraza jest dzielona na wyrazy według białych znaków i znaków specjalnych. Wszystkie wyrazy muszą pasować (AND), przy ignorowaniu wielkości liter.
- Każdy wyraz może wystąpić w dowolnym fragmencie nazwy albumu lub nazwy wykonawcy. Tagi nie wpływają na wynik tego wyszukiwania.
- Liczniki tagów albumów korzystają z tego samego zakresu wyszukiwania co lista wyników.

### Wydajność biblioteki

- Odczyty albumów i ich tagów działają bez śledzenia encji przez Entity Framework, a odpowiedź pobiera tylko dane potrzebne do widoku.
- Domyślne sortowanie albumów po nazwie korzysta z indeksu przypisań użytkownika, dzięki czemu baza wybiera stronę wyników przed dołączeniem pełnych rekordów albumów.
- Dodano migrację `20260803123000_OptimizeAlbumNamePaging`, która zmienia zapisaną nazwę albumu na `varchar(255)` i tworzy indeks dla stronicowania po nazwie.
- ID aktualnego użytkownika Spotify jest cache’owane wyłącznie w pamięci dla konkretnej wersji sesji tokenu. Zmiana, odświeżenie lub wyczyszczenie tokenu unieważnia cache, więc odpowiedź dla starej sesji nie może zostać użyta przez nowe konto.

### Filtry albumów

- Przycisk **Resetuj** na widoku albumów czyści również roboczy wybór wykonawcy.
- Reset nadal przygotowuje wyłącznie pusty stan roboczy; lista wyników i ostatnio zastosowane kryteria zmieniają się dopiero po kliknięciu **Zastosuj** lub **Filtruj**.
