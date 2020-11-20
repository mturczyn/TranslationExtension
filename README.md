# VisualStudioExtensions
Repository with extensions to Visual Studio.

Jak zainstalować rozszerzenie?
W katalogach, gdzie lądują skompilowane pliki (foldery Release albo Debug) są pliki o rozszerzeniu vsix. Są to instalatory rozszerzenia, która należy uruchomić.

Jak testować rozszerzenie?
1. Testując rozszerzenie uruchamiana jest eksperymentalna instancja Visuala, którą od czasu do czsau należy wyczyścić (ponieważ są tam dodawane nasze wtyczki/rozszerzenia i za każdym razem):
Menu start => Visual Studio 2019 => Reset the Visual Studio 2019 Experimntal Instance
Ewentualnie można sobie wygooglować jak to się robi (jeśli powyższe nie będzie działać).
2. W kataogu solucji mamy dodatkowy katalog TestSolution, na której powinniśmy testować to rozszerzenie (ma on taką samą strukturę jak nasz projekt HUF).

W solucji mamy projekt Test, gdzie testujemy sobie samo okno do tłumaczeń (żeby nie odpalać eksperymentalnej instancji VS). Jest tam skrypt pre-buildowy, który przepisuje kod okna z projektu
rozszerzenia do projektu testów, tak aby mieć aktualne okno.