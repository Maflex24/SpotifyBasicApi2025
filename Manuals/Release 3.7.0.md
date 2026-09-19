# Release 3.7.0

Porównanie zmian: `3.6.0` → `3.7.0`.

## Najważniejsze zmiany

### Albumy: grupowe filtry wykonawców

- W panelu filtrów wykonawców na **/albums** nagłówki grup literowych (`A`, `B`, … oraz `#`) są teraz klikalne.
- Kliknięcie ustawia stan wszystkich aktualnie widocznych wykonawców z danej grupy: **brak** → **uwzględnij** (zielony) → **wyklucz** (czerwony) → **brak**.
- Gdy wykonawcy w grupie mają różne stany, nagłówek pozostaje neutralny, a kliknięcie ujednolica grupę jako uwzględnioną.
- Grupowa akcja zachowuje istniejący model roboczych filtrów: wynik albumów zmienia się po użyciu przycisku **Zastosuj**, a wybory są zapamiętywane lokalnie.

### Albumy: wyszukiwanie w indeksie wykonawców

- Wysuwany indeks wykonawców w zgrupowanym widoku albumów ma lokalne wyszukiwanie nazw z dopasowaniem zawierającym fragment frazy, bez rozróżniania wielkości liter.
- Pole wyszukiwania pozostaje przy dolnej krawędzi panelu, również gdy filtrowana lista ma niewiele pozycji albo nie ma ich wcale.
- Po otwarciu indeksu fokus automatycznie trafia do pola wyszukiwania, więc można od razu pisać.
- Przycisk **×** czyści frazę, a wybór wykonawcy również ją resetuje przed przewinięciem widoku do jego sekcji albumów.
- Wyszukiwanie indeksu nie zmienia filtrów albumów, nie wykonuje zapytań API i nie jest zapisywane lokalnie.

## Zmiany techniczne i weryfikacja

- Nie są wymagane migracje bazy danych ani zmiany API.
- Wydanie zweryfikowano komendą `dotnet build Frontend/Frontend.csproj --no-restore`.
