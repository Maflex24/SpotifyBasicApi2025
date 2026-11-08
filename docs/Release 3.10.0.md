# Release 3.10.0

Porównanie zmian między `3.9.0` i `3.10.0`.

## Tagowanie na przefiltrowanych listach

- Tagi i ich wagi zapisują się od razu. Na listach albumów, artystów, utworów i utworów playlisty edytowana pozycja pozostaje na swoim miejscu z etykietą **Zmieniono tagi**.
- Wyróżniony przycisk **Zastosuj** ponownie przelicza skład i kolejność wyników. Przed kliknięciem można zmienić wagę właśnie dodanego tagu, nawet jeśli spełnia on regułę wykluczenia.
- Zmiany są widoczne również w rozwiniętych dyskografiach i przy tagach wykonawców na liście albumów. Jeśli utwór występuje na playliście kilka razy, aktualizują się wszystkie jego wystąpienia.
- Gdy zmiana tagu może wpłynąć na kolejne strony albumów lub utworów, dociąganie wznawia się po kliknięciu **Zastosuj**. Przycisk **Odtwórz wczytane** obejmuje wówczas utwory już widoczne na liście.
- Po błędzie odświeżania lista zachowuje pozycje i oznaczenia, dzięki czemu można ponowić próbę. Odpowiedź odświeżania, w trakcie którego zapisano nowszą zmianę tagu, jest odrzucana.

## Filtrowanie według wagi

- API albumów, artystów i utworów uwzględnia końcową wagę tagu przy wykluczaniu. Reguła obejmuje także tagi wykonawcy w wynikach albumów, gdy włączono ich uwzględnianie.

## Weryfikacja

- W przeglądarce z testowym API sprawdzono wszystkie cztery listy, zmianę wag, sortowanie, dyskografie, duplikaty na playlistach, paginację oraz edycję podczas odświeżania.
- `dotnet test Tests/TagDateSorting.Tests/TagDateSorting.Tests.csproj --no-restore -p:RuntimeFrameworkVersion=8.0.21` zakończyło się wynikiem 39 zaliczonych testów. Test MySQL został pominięty z powodu braku skonfigurowanego połączenia.
