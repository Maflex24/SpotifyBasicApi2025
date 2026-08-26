# Release 3.4.0

Porównanie zmian: `3.3.0` → `3.4.0`.

## Najważniejsze zmiany

### Pełna dyskografia wykonawcy w albumach

- W zgrupowanym widoku **/albums** przy grupie wykonawcy, obok przycisku **Podobni**, dostępny jest przełącznik **Przefiltrowane ▼ / Wszystkie ▲**.
- Stan **Wszystkie** pobiera ze Spotify studyjne wydania typu `album` danego wykonawcy. Pomija pozytywne kryteria, zakres lat i wyszukiwanie, lecz nadal respektuje aktywne filtry wykluczające tagów, wykonawców i gatunków.
- Pobrany katalog jest pamiętany dla rozwiniętej grupy do czasu zmiany filtrów. Po zwinięciu wraca wyłącznie lista wyników przefiltrowanych.
- W tym widoku niebieska ramka oznacza album, który pasuje bezpośrednio do aktywnych kryteriów albumu. Ułatwia to rozróżnienie go od albumów pokazanych dzięki tagom wykonawcy lub rozwiniętej dyskografii.
- Album spoza lokalnej biblioteki oferuje akcję **Dodaj do biblioteki**. Po jej powodzeniu metadane są od razu synchronizowane z aplikacją, dzięki czemu album można natychmiast tagować i dodawać do niego notatkę.

### Filtrowanie i zapisane filtry tagów

- Zapisane filtry w widokach **/albums**, **/artists**, **/tracks** i utworów playlist zachowują progi liczbowe tagów: operator **≥** lub **≤** oraz jego wartość.
- Te same progi są przywracane z lokalnego cache i prawidłowo działają także dla lokalnego filtrowania utworów playlist, w trybach OR oraz AND.
- Starsze zapisane filtry bez progów pozostają obsługiwane.
- Wczytanie filtra nie otwiera ani nie zamyka już panelu filtrowania tagów — zachowuje aktualny układ widoku. Stan panelu nadal jest pamiętany lokalnie po odświeżeniu strony.

### Losowanie i rekomendowani wykonawcy

- Kliknięcie **Losuj z wczytanych** przewija teraz płynnie do wylosowanego albumu. Przy grupowaniu wykonawców jego grupa zostanie najpierw rozwinięta.
- Samo odświeżenie widoku nie wywołuje automatycznego przewijania do wcześniej wylosowanego albumu; nadal służy do tego ręczny przycisk **Wylosowany**.
- W oknie **Fani lubią też** wyszukiwanie można zatwierdzić klawiszem Enter.
- Akcja **Dodaj** przy rekomendowanym wykonawcy dodaje go do biblioteki, importuje jego albumy studyjne i od razu uwzględnia wykonawcę w bieżącym filtrowaniu. Po odświeżeniu albumy są widoczne, o ile nie ukrywają ich pozostałe aktywne filtry.

### Kopiowanie i wklejanie tagów

- Menu z trzema kropkami przy albumach, wykonawcach i utworach zawiera akcje **Kopiuj tagi** oraz **Wklej tagi** — również bez dostępu do AI.
- Schowek kopiuje bezpośrednie tagi elementu wraz z ich wagami. Wklejenie jest dostępne dla elementów znajdujących się w bibliotece i wymaga potwierdzenia z pełną listą tagów oraz wag.
- Wklejane tagi są dodawane lub aktualizowane, a pozostałe tagi elementu docelowego pozostają bez zmian. Po sukcesie karta natychmiast pokazuje aktualny stan tagów.
- Schowek działa w bieżącej sesji karty przeglądarki i jest czyszczony po jej odświeżeniu.

## Zmiany techniczne i weryfikacja

- Dodano wspólny klientowy schowek tagów oraz wsadowe, atomowe przypisywanie tagów po stronie API.
- Nie są wymagane migracje bazy danych.
- Wydanie zweryfikowano kompilacją projektów Frontend i WebApi.
