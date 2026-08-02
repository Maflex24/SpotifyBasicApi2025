# Release 2.1.0

Porównanie zmian: `2.0.0` → `2.1.0`.

## Najważniejsze zmiany

### Filtry i tagi

- Lista tagów w przyklejonym pasku filtrów ma własne przewijanie, gdy nie mieści się w dostępnej wysokości okna. Dzięki temu można dotrzeć do wszystkich tagów bez przechodzenia do listy wyników.
- Dodano opcję **Pokaż nieprzypisane tagi**, wspólną dla albumów, artystów, utworów i utworów playlisty. Domyślnie lista filtrów pokazuje tylko tagi występujące w bieżących wynikach.
- Ustawienie widoczności nieprzypisanych tagów jest zapamiętywane lokalnie i pozostaje wspólne dla wszystkich widoków biblioteki.
- Aktywnie włączone lub wykluczone tagi są nadal widoczne, nawet jeśli ich licznik wynosi zero, aby można było je łatwo odznaczyć.
- W widoku grupowanym ukrywane są puste grupy tagów.

### Zarządzanie tagami

- Podczas edycji tagu można teraz przenieść go do innej grupy lub kategorii.

### Wygląd i czytelność

- Dopracowano zwartość oraz minimalny padding chipów tagów, aby tekst nie wychodził poza przycisk także w węższych widokach.
- Liczniki przypisań tagów otrzymały bardziej kontrastową, jasną kapsułkę z ciemnym, pogrubionym tekstem.

### Techniczne

- Endpointy liczników tagów dla albumów, artystów i utworów playlisty uwzględniają pełny aktualny zbiór kryteriów, w tym wyszukiwanie oraz zakres lat albumów.
- Liczniki tagów są odświeżane wraz z lokalnym wyszukiwaniem, aby lista dostępnych tagów odpowiadała aktualnie widocznym wynikom.
