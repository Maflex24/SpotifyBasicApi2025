# Release 2.4.0

Porównanie zmian: `2.3.0` → `2.4.0`.

## Najważniejsze zmiany

### Nazwy alternatywne tagów

- Jeden tag może mieć wiele ręcznie dodanych nazw alternatywnych. Wszystkie warianty wskazują ten sam tag, więc mają wspólne przypisania, siłę, liczniki i zapisane filtry.
- Na stronie **Tagi** formularz edycji pozwala dodawać, zmieniać i usuwać nazwy alternatywne.
- Można zamienić nazwę główną z aliasem w jednym zapisie: wystarczy przepisać wartości w formularzu. System sprawdza końcowy zestaw nazw, nie blokując tej operacji jako fałszywego duplikatu.
- Nazwy są przycinane, a wielokrotne białe znaki są upraszczane. Ta sama nazwa, bez względu na wielkość liter, nie może należeć do dwóch różnych tagów tego samego użytkownika.

### Lista i wyszukiwanie tagów

- Na **Tagach** nazwa główna i wszystkie aliasy są osobnymi wpisami sortowanymi wspólnie alfabetycznie w ramach grupy.
- Alias jest przedstawiany jako `nazwa alternatywna (nazwa główna)` i pokazuje ten sam licznik przypisań.
- Przyciski **Edytuj** i **Usuń** przy aliasie działają na jego tag główny. Usunięcie wyłącznie aliasu wykonuje się w formularzu edycji tagu.
- Wyszukiwanie obejmuje nazwy główne, aliasy i nazwy grup.

### Filtry i tagowanie

- Nazwy alternatywne są dostępne jako osobne, krótkie opcje w filtrach tagów albumów, artystów, utworów i utworów playlisty.
- Alias w filtrze nie tworzy drugiego kryterium: wybór nazwy głównej i aliasu działa na ten sam identyfikator tagu.
- Panele tagowania pokazują wszystkie warianty nazwy jako osobne opcje. Wybranie aliasu przypisuje dokładnie ten sam tag co wybór nazwy głównej.
- Badge’e przypisanych tagów i aktywne chipy filtrów nadal używają nazwy głównej, aby stan był jednoznaczny.

### Techniczne

- Dodano migrację `20260803133000_AddTagAliases`, tworzącą tabelę aliasów oraz unikalny indeks nazw alternatywnych w obrębie użytkownika.
- Zapytania wyników biblioteki, przypisań i liczników nadal pracują wyłącznie na identyfikatorze tagu; aliasy są rozwijane lokalnie po pobraniu listy tagów.
