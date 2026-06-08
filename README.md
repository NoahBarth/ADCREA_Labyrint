# ADCREA_Labyrint
Projekt für das HSLU‑Modul **ADCREA FS26**

## Einleitung
Dieses Projekt visualisiert Pfadfindungsalgorithmen in einem prozedural generierten 2D‑Labyrinth. Ziel ist, zu zeigen, warum verschiedene Algorithmen unterschiedliche Wege wählen, wie sie arbeiten und welche Datenstrukturen sie nutzen.

## Idee und Motivation
- **Prinzip:** Das Spiel erzeugt ein Labyrinth per iterativer DFS, instanziiert Spielobjekte und lässt mehrere Algorithmy Player unabhängig Pfade berechnen und ablaufen. Zusätzlich werden Sackgassen und Besuchsreihenfolgen anhand von DFS visualisiert.

## Kerngedanken / Funktionsweise
- **Generierung:** Ein `labyrinthGenerator` erzeugt ein Gitter (`Node[,]`) und verwendet eine iterative Depth‑First‑Carving‑Methode (Recursive‑Backtracker in 2‑Zellen‑Schritten). Zusätzliche Pfade können zufällig eingefügt werden, damit Schleifen entstehen.
- **Visualisierung:** Wände und Böden werden als Prefabs instanziiert; Sackgassen werden optional eingefärbt; die Kamera passt sich automatisch an die Bounds an.
- **Agenten:** Spieler‑Objekte holen sich je einen Pfad per `Pathfinder` und laufen die Wegpunkte nacheinander ab.

## Algorithmen 
- **Dijkstra:** Findet den kürzesten Pfad in einem gewichteten Graphen (hier Gitter mit Einheitendistanzen) indem es sukzessive den Knoten mit den geringsten aktuellen Kosten auswählt. Garantiert kürzeste Wege ohne Heuristik.
- **A-star :** Erweiterung von Dijkstra mit einer Heuristik `h(n)` zur Abschätzung der Restkosten. Führt meist schneller zum Ziel, wenn die Heuristik zulässig ist (z. B. Manhattan/Euklid).
- **DFS — Depth‑First Search:** Traversiert so tief wie möglich und backtrackt bei Sackgassen. Findet einen Pfad, aber nicht notwendigerweise den kürzesten. Nützlich zur Visualisierung alternativer Wege und zur Erkennung von Sackgassen.

## Verwendete Datenstrukturen (Rolle im Projekt)
- **2D‑Array (`Node[,]`):** Primärer Speicher des Gitters; erlaubt schnellen indexed Zugriff auf Kacheln.
- **Dictionary <Vector2Int, Node> (`nodeLookup`):** O(1) Position→Node Lookup für Nachbarschaftsabfragen und World‑Mapping.
- **List:** Temporäre Sammlungen (Open‑List, Nachbarn, Floor‑Knoten). Kombiniert mit `Sort()` zur Auswahl des nächsten Knotens.
- **Stack:** Wird genutzt, um rekonstruierten Pfad umzukehren (Ziel→Start → Stack → Pop liefert Start→Ziel). Ebenfalls Kernstruktur der iterativen DFS‑Generierung.
- **BST / VisitTracker:** Binary Search Tree zur chronologischen Speicherung von Besuchszeitpunkten (zum Visualisieren der Besuchsreihenfolge).
- **HashSet:** Zur schnellen Mitgliedschaftsprüfung (z. B. Closed‑Set beim Pfadfinden oder besuchte Knoten bei DFS).

Hinweis: Die klassischen Datenstrukturen und ihre Darstellung sind in der Algorithmenliteratur (z. B. Donald Knuth: The Art of Computer Programming) ausführlich beschrieben.


## Starten / Ausführen
1. Unity öffnen (empfohlene Version: 6000.3.8f1)
2. Projekt öffnen und Szene `Assets/Scenes/SampleScene.unity` laden
3. Auf Play drücken — der Generator baut das Labyrinth und die Agenten starten automatisch.

## Autoren & Credits
- Sandro Fankhauser & Noah Barth — Implementierung und Visualisierung