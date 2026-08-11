# Release 3.0.0

Porównanie zmian: `2.8.0` → `3.0.0`.

## Najważniejsze zmiany

### Albumy: tryb Blind

- Widok **/albums** ma trwały lokalnie przełącznik **Tryb Blind**. Ukrywa on dane Spotify — nazwy, okładki, daty wydania, gatunki i linki do szczegółów — zastępując je identyfikatorami i neutralnymi placeholderami.
- Prywatne tagi, notatki oraz filtr roku pozostają dostępne. Przełącznik zachowuje wybrany stan po odświeżeniu widoku.
- Odtwarzany album jest wyróżniany zieloną ramką zarówno w Blind, jak i w zwykłym widoku.

### Albumy: losowanie i nawigacja

- **Losuj** w trybie Blind pokazuje wyłącznie ID albumu i zawęża widok do wylosowanej karty, bez ujawniania poprzedniej frazy wyszukiwania.
- Dodano **Losuj z wczytanych**: wybiera album tylko spośród aktualnie pobranych kart, oznacza go na żółto, przewija do niego widok i zapamiętuje ID lokalnie. Po odświeżeniu lista doczytuje strony do odnalezienia wybranego albumu.
- Przy grupowaniu po wykonawcy grupa z lokalnie wylosowanym albumem jest automatycznie rozwijana. Dodano także strzałki nawigacji między albumami.
- **Odtwórz wszystko** jest dostępne tylko dla maksymalnie pięciu unikalnych albumów; większa lista nie może zostać przypadkowo dodana do kolejki.

### Player

- Dolny player respektuje tryb Blind na trasie **/albums**: pokazuje ID utworu i albumu zamiast nazw oraz nie ujawnia okładki.
- W Blind player pokazuje również pozycję utworu w albumie, np. `6/12`.
- Dodano akcje **Taguj album** i **Notatka albumu** dla aktualnie odtwarzanego albumu, bez potrzeby szukania jego karty.
- Pasek postępu utworu jest klikalnym suwakiem — można przeskoczyć do wybranego momentu nagrania myszą, dotykiem lub klawiaturą.

### Biblioteka i wykonawcy

- Dodano prywatne notatki do albumów i wykonawców.
- Wykonawcy mogą otrzymywać tagi; dostępne są także ich gatunki oraz filtrowanie albumów po gatunkach i tagach wykonawców.
- Panel podobnych wykonawców umożliwia wyszukanie, podgląd, odtworzenie oraz dodanie do biblioteki wybranych propozycji.
- Lista albumów pobiera do 500 pozycji na stronę, ograniczając liczbę kolejnych zapytań podczas przeglądania dużej biblioteki.

## Zmiany techniczne

- Dodano trwałe notatki elementów biblioteki wraz z migracją bazy danych.
- Stan trybu Blind, lokalnie wylosowanego albumu i aktualnie odtwarzanego albumu jest zarządzany po stronie frontendu, bez ujawniania metadanych Spotify w trybie anonimowym.
