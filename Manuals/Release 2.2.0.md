# Release 2.2.0

Porównanie zmian: `2.1.0` → `2.2.0`.

## Najważniejsze zmiany

### Świadome stosowanie filtrów

- Widoki **Albumy**, **Artyści**, **Utwory** i **utwory playlisty** rozdzielają teraz stan roboczy filtrów od ostatnio zastosowanego stanu.
- Zmiana wyszukiwania, tagów, trybu AND/OR, sortowania lub pozostałych kryteriów nie odświeża wyników automatycznie. Pasek aktywnych kryteriów pokazuje stan **Oczekuje zastosowania** oraz przycisk **Filtruj**.
- Górny przycisk **Zastosuj** i przycisk **Filtruj** wykonują tę samą akcję. Reset przygotowuje pusty zestaw kryteriów; przycisk w komunikacie o braku wyników resetuje je i od razu stosuje.
- Wczytanie zapisanego filtra nadal jest akcją natychmiastową.
- Sortowanie nagłówkami tabel utworów i playlisty jest etapowane do czasu zastosowania filtrów. Infinite scroll na liście utworów korzysta wyłącznie z ostatnio zastosowanego stanu.
- Filtrowanie playlisty po zastosowaniu odbywa się lokalnie na już pobranych utworach, bez ponownego pobrania całej playlisty.

### Wyniki i puste widoki

- Nagłówki bibliotek pokazują liczbę wyników na widokach albumów, artystów, utworów, playlist i utworów playlisty.
- Gdy zastosowane kryteria nie dają wyników, widok pokazuje krótki komunikat oraz przycisk **Resetuj filtry**.
- Komunikaty są responsywne i na wąskich ekranach układają akcję pod opisem.

### Wyszukiwanie tagów

- Na stronie **Tagi** dodano lokalne wyszukiwanie po nazwie tagu i grupy. Dopasowanie do grupy pokazuje wszystkie jej tagi, a grupy bez wyników są ukrywane.
- Panele filtrów tagów w albumach, artystach, utworach i playlistach mają zwięzłe, lokalne pole **Szukaj tagu** z przyciskiem `×` do wyczyszczenia frazy. Nie wpływa ono na wyniki biblioteki ani na stan oczekujący filtrów.
- Panel tagowania pozwala wyczyścić pozostawioną po wyborze frazę wyszukiwania przyciskiem `×` po lewej stronie pola. Ta akcja nie usuwa przypisanego tagu.

### Tagowanie i interfejs

- Po udanej zmianie tagów albumu jego karta otrzymuje krótkie zielono-morskie podświetlenie, a widoczne badge’e tagów delikatnie pulsują. Efekt respektuje ustawienie systemowe ograniczenia ruchu.
- Wspólny panel tagowania ma maksymalną szerokość równą belkom filtrów. Przy widocznym sidebarze jest centrowany w obszarze treści i automatycznie zwęża się wraz z dostępną szerokością.
- Ujednolicono klasy paska aktywnych filtrów, zachowując wygląd dotychczasowego paska albumów oraz poprawiając zachowanie przycisków w responsywnych toolbarach.

### Dodatkowe kryteria

- Lista utworów udostępnia pole filtrowania po roku wydania.
