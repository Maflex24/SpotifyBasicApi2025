# Release 3.6.0

Porównanie zmian: `3.5.0` → `3.6.0`.

## Najważniejsze zmiany

### Historia przypisań tagów

- W głównym menu dostępna jest nowa strona **Historia przypisań**.
- Historia zapisuje zmiany przypisań tagów wykonane przez bieżącego użytkownika: dodanie tagu, usunięcie tagu oraz zmianę siły przypisania.
- Obejmuje także zbiorcze wklejanie tagów oraz usuwanie tagów lub całych grup tagów.
- Wpisy zachowują migawkę tagu, jego grupy i elementu, dlatego pozostają czytelne również po późniejszym usunięciu tagu, grupy albo elementu z biblioteki.
- Dostępne są filtry typu elementu, tagu i rodzaju akcji, licznik wyników oraz paginacja. Najnowsze zmiany są wyświetlane jako pierwsze.

### Miniatury elementów

- W kolumnie **Element** historia pokazuje miniaturę 36 × 36 px bez zwiększania wysokości wiersza.
- Dla wykonawców używany jest ich obraz, dla albumów i playlist okładka, a dla utworów preferowana jest okładka albumu.
- Miniatura jest migawką z chwili zapisania zdarzenia. Brak obrazu albo błąd jego ładowania wyświetla neutralny placeholder.
- Miniatura i nazwa prowadzą do istniejących widoków szczegółów wykonawcy, albumu lub playlisty. Utwory pozostają bez linku, ponieważ nie mają osobnego widoku szczegółów.

## Zmiany techniczne i wdrożenie

- Dodano dziennik `LibTagAssignmentHistories`, per użytkownik, przechowujący historię bezterminowo. Daty są zapisywane w UTC i prezentowane lokalnie w interfejsie.
- Migracja `20260913000000_AddTagAssignmentHistory` tworzy strukturę historii oraz jednorazowo importuje obecne przypisania jako zdarzenia dodania z datą utworzenia przypisania.
- Migracja `20260913000001_AddTagAssignmentHistoryImageUrl` dodaje migawkę adresu miniatury i uzupełnia obrazy dla istniejącej historii, jeżeli metadane Spotify są dostępne.
- Druga migracja jest bezpieczna po częściowo wykonanej poprzedniej próbie na MariaDB: nie próbuje ponownie dodać istniejącej kolumny `ImageUrl` i uzupełnia wyłącznie puste wartości.
- Po wdrożeniu należy uruchomić aplikację lub wykonać standardowe migracje EF Core, aby obie migracje zostały zastosowane na bazie danych.
