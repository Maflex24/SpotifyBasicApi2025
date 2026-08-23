# Release 3.1.0

Porównanie zmian: `3.0.0` → `3.1.0`.

## Najważniejsze zmiany

### Filtry tagów według wagi

- Filtry tagów na listach **artystów**, **albumów**, **utworów** oraz utworów playlisty mogą teraz uwzględniać wagę przypisania.
- Dla każdego wybranego taga można niezależnie przełączać warunek: **≥**, **≤** lub **∞** (dowolna waga). Progi mają zakres od `0,5` do `6` ze skokiem co `0,5`.
- Warunek działa zarówno dla tagów włączanych, jak i wykluczanych oraz z trybami łączenia **AND** i **OR**. Bez wybranego progu zachowane jest dotychczasowe filtrowanie po samym tagu.
- Aktywne filtry pokazują wybrany operator i wartość wagi. Reguły są zachowywane w zapisanych filtrach oraz w oczekujących zmianach filtrów.

### Widoczność tagów na kartach i w playliście

- Karty albumów, karty artystów i wiersze utworów playlisty domyślnie pokazują maksymalnie trzy tagi.
- Przycisk `+N` pokazuje po najechaniu lub fokusie dymek z ukrytymi tagami i ich wagami.
- Kliknięcie `+N` rozwija pełną listę; przycisk **Pokaż mniej** pozwala ją ponownie zwinąć. Stan rozwinięcia jest lokalny dla konkretnej karty lub wiersza.
- W kartach rozwinięcie zachowuje równą wysokość elementów w tym samym wierszu siatki. Kliknięcie taga artysty nadal otwiera jego dotychczasową akcję edycji.

### Tryb Blind dla albumów

- W widoku **/albums** tryb Blind umożliwia pojedyncze ujawnienie nazwy albumu, wykonawcy, roku wydania albo okładki.
- Każde ujawnienie wymaga osobnego potwierdzenia. Anulowanie pozostawia informację ukrytą, a ujawnienie jednego pola nie odkrywa pozostałych.
- Przycisk **Rok** jest dostępny także wtedy, gdy rok wydania nie istnieje; po potwierdzeniu wyświetla informację o braku danych.
- Ujawnienia są przechowywane wyłącznie w pamięci bieżącego widoku: po odświeżeniu lub ponownym wejściu w tryb Blind dane znów są ukryte.

### Tytuły kart i ikonka aplikacji

- Tytuły kart przeglądarki mają format **Library Manager – nazwa widoku**, np. **Library Manager – Albumy** lub **Library Manager – Artyści**.
- Dodano również polskie tytuły widoków szczegółów, logowania i strony nieznalezionej.
- Aplikacja otrzymała lokalny favicon SVG przedstawiający nutę i listę porządkowania, bez zależności od zewnętrznego dostawcy.

## Zmiany techniczne

- Endpointy biblioteki dla artystów, albumów i utworów obsługują opcjonalne parametry `includeTagStrengths` i `excludeTagStrengths` w formacie `tagId:gte|lte:wartość`.
- Backend waliduje operator, wagę, przypisanie reguły do wybranego taga i duplikaty. Nieprawidłowy parametr zwraca `400 Bad Request`.
- Wspólna logika backendu stosuje progi wag w zapytaniach dla artystów, albumów i utworów; przy albumach z tagami wykonawców sprawdzana jest waga rzeczywiście dopasowanego przypisania.
- Dodano wspólne komponenty frontendowe do obsługi filtra tagów z wagą oraz podglądu nadmiarowych tagów.
