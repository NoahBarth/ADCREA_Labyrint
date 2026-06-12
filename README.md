Kurzbeschreibung Labyrint-Algorithmus-Solver

Das Ziel des folgendne Projekts ist es, die Lösungen unterschiedlicher Algorithmen innerhalb eines Labyrints visuell Darzustellen und nachzuvollziehen. Man sieht auch denn direkten Vergleich zwischen AStar, Dijkstra und DFS.

Das Projekt erstellt basierend auf Benutzerdefinierten Angaben am labyrinthGenerator (Inspector) aus zwei Prefabs via DFS ein Labyrint, in welchem es durch 3 Algorithmen Paths vom Start bis ans Ziel Visualisiert.
Die Pfad-Visualisierung kann mittels Toggle an oder ausgeschalten werden. Zusätzlich kann der Benutzer auch die Grösse festlegen, sowie die Anzahl der möglichen Wege zum Ziel erhöhen.

Die einzelnen Tiles sind gemäss ihrer Gewichtung von Hell- bis Dunkelbraun eingefärbt. Standartmässig ist der Dijkstra-Algorithmus rot, DFS Grün und AStar Blau. Dies kann vom Benutzer farblich überschrieben werden.

AStar und Dijkstra liefern in den meisten Fällen denselben "besten" Weg zum Ziel, dies jedoch mit weniger Aufwand beim AStar-Algorithmus. 