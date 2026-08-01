# Release 2.0.0

Porównanie zmian: `1.7.0` → `2.0.0`.

## Najważniejsze zmiany

### Filtry i tagi

- Ujednolicono pasek filtrów na widokach albumów, artystów, utworów i utworów playlisty.
- Pasek filtrów jest przyklejany podczas przewijania: po minięciu jego punktu początkowego można go zwinąć do samej strzałki, a po powrocie na górę rozwija się automatycznie.
- Dopracowano responsywny układ pól, akcji, zapisanych filtrów i sekcji tagów.
- Tagi filtrów otrzymały jednolity wygląd chipów oraz opcję grupowania według grup tagów.
- Sekcję tagów na albumach umieszczono w osobnym, czytelnym panelu.

### Biblioteka i odtwarzanie

- Dodano akcję **Odtwórz wszystko** dla albumów.
- Dodano możliwość dodawania albumu do kolejki odtwarzania.
- Dodano losowy wybór albumu i artysty.
- Poprawiono prezentację kart albumów i artystów: bardziej zwarty rozmiar okładek, lepiej widoczne akcje oraz subtelniejszy przycisk odtwarzania.

### Tagowanie i wydajność

- Panel tagowania używa spójnego stylu przycisków i tagów, zgodnego z filtrami biblioteki.
- Endpoint `GET /api/tags/assign/{itemType}/{spotifyItemId}` zwraca pełny stan przypisań tagów: identyfikator, nazwę, grupę i wagę.
- Po dodaniu, usunięciu lub zmianie wagi tagu odświeżane są wyłącznie tagi zmienionego albumu, artysty, utworu lub playlisty — bez ponownego pobierania całej listy.
- Widoki lokalnie aktualizują tagi, filtrowanie, sortowanie i liczniki po zmianie przypisań.

### Techniczne

- Rozszerzono obsługę kolejki i odtwarzania po stronie API oraz serwisu odtwarzacza.
- Wprowadzono wspólne komponenty pomocnicze dla ikon i stanu przypisań tagów.
- Dodano dokument orientacyjny projektu dla pracy wspieranej przez AI.
