# Release 2.8.0

Porównanie zmian: `2.7.0` → `2.8.0`.

## Najważniejsze zmiany

### Albumy: grupowanie po wykonawcy

- Przy sortowaniu albumów według wykonawcy dostępny jest przełącznik **Grupuj**. Dzieli on wyniki na sekcje wykonawców, a albumy w każdej sekcji układa chronologicznie od najstarszego.
- Album z kilkoma wykonawcami pojawia się w każdej odpowiadającej mu sekcji. Akcja **Odtwórz wszystko** nadal dodaje taki album do kolejki tylko raz.
- Ustawienie grupowania jest zapamiętywane lokalnie i pozostaje dostępne po ponownym wybraniu sortowania według wykonawcy.
- Lista albumów pobiera teraz do 500 pozycji w jednej stronie, zanim uruchomi doczytywanie kolejnej strony.

### Albumy: filtr wykonawców

- Pojedynczy wybór wykonawcy zastąpiono panelem **Wykonawcy** z kafelkami o trzech stanach: neutralnym, zielonym (uwzględnij) i czerwonym (wyklucz).
- Zielone kafelki wymagają co najmniej jednego pasującego wykonawcy albumu, a czerwone odrzucają album zawierający dowolnego z wykluczonych wykonawców. Bez zaznaczeń filtr wykonawców nie ogranicza wyników.
- Akcje **Wszyscy** i **Żadni** pozwalają szybko zaznaczyć lub wyczyścić widocznych wykonawców. Wybory są zachowywane lokalnie i w zapisanych filtrach; starsze zapisane filtry z pojedynczym wykonawcą pozostają obsługiwane.
- Lista wykonawców reaguje na zastosowane wyszukiwanie, tagi i zakres lat, ale nie zawęża się własnymi zaznaczeniami.
- Kafelki wykonawców są podzielone alfabetycznie, z osobną grupą `#` dla nazw niealfabetycznych. Długa lista ma własne przewijanie, dzięki czemu wyszukiwarka i przyciski akcji pozostają widoczne.
