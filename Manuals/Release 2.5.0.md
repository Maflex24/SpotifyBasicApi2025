# Release 2.5.0

Porównanie zmian: `2.4.0` → `2.5.0`.

## Najważniejsze zmiany

### Trwałe losowanie albumów

- Przycisk **Losuj** na stronie **Albumy** wybiera teraz album po stronie backendu z pełnego zbioru wyników spełniających zastosowane kryteria, a nie tylko spośród aktualnie wczytanych kart.
- Po losowaniu widok przechodzi do jednego albumu w sposób odporny na przewijanie, infinite-scroll i odświeżenie strony.
- Pole wyszukiwania otrzymuje frazę zawierającą wcześniejsze wyszukiwanie, nazwę wykonawcy, nazwę albumu oraz pełne ID Spotify albumu. ID jednoznacznie utrzymuje wybrany rekord nawet przy identycznych nazwach albumów.
- W trybie losowania przycisk zmienia opis na **Losuj ponownie**. Kolejne losowanie korzysta z pierwotnej frazy użytkownika, bez zawężania puli przez poprzednio wylosowany album.
- Dodano akcję **Wróć do wyników**, która przywraca wcześniejszą frazę i od razu ładuje pełny zestaw wyników.
- Stan losowania i pierwotna fraza są zachowywane lokalnie, więc odświeżenie strony pozostawia wylosowany album.

### Wyszukiwanie albumów

- Backendowe wyszukiwanie albumów oraz liczniki tagów uwzględniają również Spotify ID albumu.
- Nadal obowiązuje wyszukiwanie bez rozróżniania wielkości liter, z warunkiem AND między tokenami i dopasowaniem do nazwy albumu, wykonawcy lub ID.

### Interfejs

- Przy wypełnionym polu wyszukiwania albumów pojawia się po jego lewej stronie przycisk **×** do szybkiego wyczyszczenia roboczej frazy.
- Czyszczenie frazy zachowuje etapowany model filtrów: wyniki nie są przeładowywane przed kliknięciem **Zastosuj**.

### Zachowanie filtrów

- Ręczna zmiana wyszukiwania, tagów, wykonawcy, lat lub sortowania, a także reset i wczytanie zapisanego filtra kończą tryb losowania.
- Doczytywanie kolejnych stron po losowaniu pozostaje związane z zastosowanym snapshotem filtrów i nie miesza poprzednich kart z wylosowanym albumem.
