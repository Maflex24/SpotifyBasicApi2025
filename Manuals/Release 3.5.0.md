# Release 3.5.0

Porównanie zmian: `3.4.0` → `3.5.0`.

## Najważniejsze zmiany

### Albumy: indeks wykonawców

- Przy sortowaniu według **Wykonawcy** i włączonym grupowaniu w widoku **/albums** dostępna jest po lewej stronie wysuwana lista wykonawców z bieżących wyników filtrowania.
- Lista jest sortowana alfabetycznie według polskiego porządku i pokazuje liczbę albumów w każdej grupie.
- Kliknięcie wykonawcy płynnie przewija widok do jego sekcji albumów. Jeżeli grupa była zwinięta, zostanie najpierw rozwinięta.
- Indeks można otworzyć i zamknąć strzałką. Po kliknięciu poza panelem zamyka się automatycznie; wybór wykonawcy nie zamyka listy, aby ułatwić kolejne skoki.
- Pierwszy wykonawca każdej nowej litery ma dodatkowy, niewielki odstęp. Wpisy zaczynające się cyfrą lub znakiem należą do grupy `#`.
- Panel nie jest wyświetlany w trybie Blind, aby nie ujawniać listy wykonawców.

## Zmiany techniczne i weryfikacja

- Indeks korzysta z istniejących danych grup albumów i mechanizmu przewijania; nie wymaga zmian API ani migracji bazy danych.
- Wydanie zweryfikowano komendą `dotnet build Frontend/Frontend.csproj --no-restore`.
