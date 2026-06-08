# CLAUDE.md

Diese Datei gibt Claude Code (claude.ai/code) Hinweise für die Arbeit mit dem Code in diesem Repository.

## Projekt

Unity 6 (6000.3.8f1) 2D-Top-Down-Labyrinthlöser für das HSLU-Modul **ADCREA** (Algorithmen & Datenstrukturen / Creative Computing). Der Spieler navigiert automatisch durch ein prozedural generiertes Labyrinth mithilfe von Pfadfindungs-Algorithmen. Erstellt von Sandro Fankhauser und Noah Barth.

## Anforderungen

### Datenstrukturen
- **2D-Array**: Zur Speicherung und Initialisierung aller Bodenkacheln.
- **List**: Resultat der Pfadsuche und Backtracking.
- **Stack**: Zur Speicherung der berechneten Route, damit der Spieler die Wegpunkte nacheinander abarbeiten kann. (Dijkstra/A* liefern den Pfad in Rückwärts-Reihenfolge; ein Stack dreht ihn beim `Pop()` in die vom Spieler benötigte Vorwärts-Reihenfolge. Eine Queue wäre hier falsch 
- **Dictionary**: Für eine schnelle Koordinatenabfrage (Vector2 zu Node), um effizient zwischen Wänden und begehbaren Wegen zu unterscheiden.

### Algorithmen
- **Dijkstra & SortedList & HashSet**: Als Standard-Algorithmus, um zuverlässig den kürzesten Weg zum Ausgang zu finden.
- **DFS (Depth-First Search / Tiefensuche)**: Um alternative Pfade zu visualisieren oder Sackgassen im Labyrinth aufzuzeigen.
- **BST (Binary Search Tree / Binärer Suchbaum)**: Jeder besuchte Knoten wird inklusive Zeitstempel eingefügt, um die Besuchsreihenfolge strukturiert abzubilden.

## Starten und Ausführen

Das Projekt in **Unity 6000.3.8f1** öffnen und im Editor auf Play drücken. Es gibt keinen CLI-Build-Schritt und keinen Test-Runner — sämtliche Iteration findet im Unity-Editor statt. Die Hauptszene ist `Assets/Scenes/SampleScene.unity`; eine prefab-basierte Variante liegt unter `Assets/LabyrinthPrefab.unity`.

## Architektur

Die gesamte Spiellogik liegt in sechs Skripten unter [Assets/Scripts/](Assets/Scripts/).

### Datenmodell — [Node.cs](Assets/Scripts/Node.cs)
Einzelne Gitterzelle. Hält `Vector2Int position`, `TileType` (Wall / Floor), eine Referenz auf das instanziierte `GameObject` sowie die Pfadfindungs-Zustandsfelder `gCost`, `hCost` und `parent`. Implementiert `IComparable<Node>`, damit Knoten nach den F-Kosten (`gCost + hCost`) sortiert werden — darauf bauen die `Sort()`-Aufrufe der Open-List auf.

### Labyrinth-Generierung — [labyrinthGenerator.cs](Assets/Scripts/labyrinthGenerator.cs)
Läuft in `Start`. Ablauf:
1. `GenerateGrid()` — legt `Node[width, height]` an (2D-Array, primäre Struktur) und füllt `Dictionary<Vector2Int, Node> nodeLookup` für O(1)-Positionszugriff.
2. `GenerateLabyrinth()` — iterative DFS (Recursive-Backtracker) mit einem `Stack<Node>`, schneidet Pfade in 2-Zellen-Schritten. Die Dimensionen werden auf ungerade Werte gezwungen, damit immer eine geschlossene Aussenwand von einer Kachel Breite erhalten bleibt.
3. `AddEntryAndExit()` — Eingang fix bei `(0, 1)`; Ausgang zufällig an der rechten Wand, angrenzend an eine bestehende Bodenkachel.
4. `CarveAdditionalPaths()` — entfernt zufällig Wände, die zwei Bodenbereiche trennen, und erzeugt so Schleifen und mehrere Lösungswege (Anzahl über `additionalPaths`).
5. `AddEntryAndExit()` — siehe oben.
6. `VisualizeLabyrinth()` — instanziiert Wand-/Boden-Prefabs; fügt Wandkacheln zur Laufzeit `Rigidbody2D` (Kinematic, Gravitation 0) und `BoxCollider2D` hinzu.
7. `SpawnPlayer()` — instanziiert den Spieler am Startknoten und hängt `Pathfinder` + `PlayerController` an.

Öffentliche Hilfsmethoden: `GetFloorNodes()` (alle begehbaren Zellen), `GetWalkableNeighbors(node)` (begehbare 1-Schritt-Nachbarn orthogonal via `nodeLookup`), `GetWorldPosition(node)` (Gitter → Weltkoordinaten). Ist `visualizeDeadEnds` aktiv, färbt `RunDeadEndVisualization()` nach der Generierung alle Sackgassen-Korridore ein.

Im Inspector konfigurierbar: `width`, `height`, `wallPrefab`, `floorPrefab`, `tileSize`, `tileOffset`, `additionalPaths`, `playerPrefab`, `visualizeDeadEnds`, `deadEndColor`.

### Pfadfindung — [Pathfinder.cs](Assets/Scripts/Pathfinder.cs)
Zwei öffentliche Methoden, beide nehmen den `labyrinthGenerator` entgegen und liefern einen `Stack<Node>` in **Vorwärts-Reihenfolge** (Start → Ziel):

- `FindPath(start, goal, generator)` — Dijkstra
- `FindPathAStar(start, goal, generator)` — A* (ergänzt eine Manhattan-/Euklid-Heuristik über `hCost`)

Beide nutzen `HashSet<Node> closedSet` und `List<Node> openList`, die pro Iteration via `openList.Sort()` sortiert wird. Das Backtracking übernimmt `BacktrackPath`: läuft die `parent`-Zeiger von Ziel → Start in eine `List<Node>`, schiebt dann jeden Knoten auf einen `Stack<Node>` und kehrt ihn so in die Vorwärts-Reihenfolge um.

Eine dritte Methode `FindPathDFS(start, goal, generator, out deadEndNodes)` implementiert eine iterative **DFS** mit `Stack<Node>` plus `cameFrom`-Dictionary und liefert den Pfad als `Stack<Node>` in Vorwärts-Reihenfolge. Alle drei Pfadfinder nutzen echte Gitter-Adjazenz über `labyrinthGenerator.GetWalkableNeighbors(node)` — die begehbaren 1-Schritt-Nachbarn orthogonal, nachgeschlagen im `nodeLookup`.

`FindDeadEnds(generator)` ist der Sackgassen-Detektor für die Visualisierung: eine Bodenzelle mit genau einem begehbaren Nachbarn (Grad 1, ausser Start/Ziel) ist ein Sackgassen-Ende; von dort wird der Korridor bis zur nächsten Kreuzung (Grad ≥ 3) verfolgt. Ist der Inspector-Schalter `visualizeDeadEnds` des Generators aktiv, werden diese Korridore nach der Generierung in `deadEndColor` eingefärbt (siehe `RunDeadEndVisualization`/`TintNode`).

Enthält ausserdem `VisitTracker` (basierend auf einem eigenen `BSTNode`-BST, geschlüsselt auf `float`-Zeitstempel), um die Besuchsreihenfolge der Knoten chronologisch festzuhalten.

### Spieler — [PlayerController.cs](Assets/Scripts/PlayerController.cs)
Steuert den selbstlösenden Spieler. `RequireComponent` stellt sicher, dass ein `Pathfinder` und ein `SpriteRenderer` vorhanden sind. In `Awake` wird ein 1×1-weisses Sprite erzeugt, falls keines gesetzt ist (rot, auf 0.8 skaliert). Die `Start`-Coroutine wartet einen Frame, bis die Generierung fertig ist, ruft dann `FindPathAStar(StartNode, GoalNode, generator)` auf und läuft den zurückgegebenen `Stack<Node>` Wegpunkt für Wegpunkt über `MoveAlongPath` ab — jeder Knoten wird gepoppt und mit `Vector3.MoveTowards` bei `moveSpeed` angesteuert. `SetGenerator(gen)` verdrahtet die Generator-Referenz (aufgerufen aus `labyrinthGenerator.SpawnPlayer`).

### Kachel-Metadaten — [TileData.cs](Assets/Scripts/TileData.cs)
Minimales `MonoBehaviour` auf den Kachel-Prefabs mit einem einzigen `bool isWall`. `VisualizeLabyrinth` liest es aus, um zu entscheiden, ob eine Kachel den Spieler blockieren soll (Fallback auf den `TileType` des Knotens, falls kein `TileData` vorhanden ist).

### Kamera — [CameraController.cs](Assets/Scripts/CameraController.cs)
Wartet nach `Start` per Coroutine einen Frame (damit die Generierung abgeschlossen ist) und passt dann die orthografische Hauptkamera automatisch so an, dass sie alle Renderer-Bounds unterhalb der Labyrinth-Wurzel umfasst — mit konfigurierbarem Rand (Padding).

## Wichtige Design-Entscheidungen

- **2D-Array + Dictionary**: `grid[x,y]` ist der kanonische Speicher; `nodeLookup` bietet positionsbasierten Zugriff ohne Iteration.
- **Stack als Pfad-Resultat**: Dijkstra/A* rekonstruieren den Pfad rückwärts; das Schieben auf einen Stack kehrt ihn natürlich um, sodass der Spieler die Wegpunkte per `Pop()` in Vorwärts-Reihenfolge abarbeiten kann.
- **List + Sort() für die Open-List**: erfüllt die Anforderung; ein `SortedSet` oder eine `SortedList` wäre O(log n) pro Einfügung statt O(n log n) pro Sortierung und ist das bevorzugte Upgrade, falls Performance wichtig wird.
- **Ungerade Dimensionen erzwungen**: Die DFS schneidet in 2-Zellen-Schritten, weshalb gerade Grössen die Aussenwand zerstören — der Generator erhöht width/height automatisch und gibt eine Warnung aus.
