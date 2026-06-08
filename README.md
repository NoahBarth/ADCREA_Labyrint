# ADCREA_Labyrint
**Modul:** ADCREA FS26 – Algorithms and Data Structures for Creative Technologies  
**Hochschule:** Hochschule Luzern - Departement Informatik
**Entwickler:** Sandro Fankhauser & Noah Barth  
**Abgabe:** 15. Juni 2026

---

## Projektbeschreibung / Data Story

Ein prozedural generiertes 2D-Top-Down-Labyrinth, das als funktionaler Demonstrator für Pfadfindungsalgorithmen und Datenstrukturen dient. Das Labyrinth wird bei jedem Start zufällig neu erzeugt. Anschliessend berechnen mehrere autonome Spieler-Agenten ihren Weg vom Eingang zum Ausgang — jeder mit einem anderen Algorithmus. Durch die parallele Visualisierung wird unmittelbar sichtbar, warum Dijkstra den kürzesten Weg findet, während DFS einen anderen (nicht notwendigerweise optimalen) Pfad wählt. Sackgassen werden zusätzlich eingefärbt, um die Struktur des Labyrinths erfahrbar zu machen.

---

## Verwendete Datenstrukturen

Datenstrukturen sind die Grundlage dafür, wie ein Programm Informationen organisiert, speichert und abruft. Die Wahl der richtigen Datenstruktur beeinflusst direkt die Effizienz und Lesbarkeit des Codes. In diesem Projekt kommen verschiedene Datenstrukturen zum Einsatz, die jeweils für eine spezifische Aufgabe optimiert sind: das **2D-Array** eignet sich für den direkten, indexbasierten Zugriff auf ein rechteckiges Gitter; ein **Dictionary** ermöglicht O(1)-Lookups nach Koordinate; ein **Stack** kehrt die Pfadreihenfolge natürlich um (LIFO); eine **List** dient als flexible Sammlung mit Sortierfunktion; ein **HashSet** prüft Mitgliedschaft in konstanter Zeit; und ein selbst implementierter **BST** speichert Besuchszeitpunkte sortiert für die Visualisierung der Traversierungsreihenfolge.

| Datenstruktur | Klasse / Datei | Rolle im Projekt |
|---|---|---|
| **2D-Array** `Node[,]` | `labyrinthGenerator.cs` | Primärer Gitterspeicher; indexbasierter Zugriff auf alle Kacheln |
| **List** `List<Node>` | `Pathfinder.cs` | Open-List beim Pfadfinden; kombiniert mit `Sort()` zur Knotenauswahl |
| **Stack** `Stack<Node>` | `Pathfinder.cs`, `labyrinthGenerator.cs` | Pfad-Umkehr (Ziel→Start in Vorwärtsreihenfolge); iterative DFS-Generierung |
| **Dictionary** `Dictionary<Vector2Int, Node>` | `labyrinthGenerator.cs` | O(1)-Positionslookup für Nachbarschaftsabfragen (nodeLookup) |
| **HashSet** `HashSet<Node>` | `Pathfinder.cs` | Closed-Set beim Pfadfinden; O(1)-Mitgliedschaftsprüfung |
| **BST** (eigene Implementierung) | `BSTNode.cs`, `Pathfinder.cs` | VisitTracker: chronologische Speicherung aller besuchten Knoten via float-Zeitstempel |

---

## Verwendete Algorithmen

### 1. Dijkstra (`FindPathDijkstra`)
Findet garantiert den kürzesten Weg vom Start zum Ziel in einem Graphen mit nicht-negativen Kantengewichten. Arbeitet mit einer Open-List (`List<Node>` + `Sort()`), einem Closed-Set (`HashSet<Node>`) und `parent`-Zeigern zur Pfadrekonstruktion. Der rekonstruierte Pfad wird per `BacktrackPath` auf einen `Stack<Node>` gelegt und kehrt so die Rückwärtsreihenfolge in Vorwärtsreihenfolge um.

### 2. DFS – Depth-First Search (`FindPathDFS`)
Iterative Tiefensuche mit `Stack<Node>` und `cameFrom`-Dictionary. Traversiert so tief wie möglich und backtrackt bei Sackgassen. Findet einen Pfad, nicht notwendigerweise den kürzesten. Wird zusätzlich zur Sackgassendetektion verwendet: Bodenzellen mit genau einem begehbaren Nachbarn werden als Sackgassenende identifiziert und der Korridor bis zur nächsten Kreuzung eingefärbt.

### 3. BST – Binary Search Tree (`BSTNode`, `VisitTracker`)
Eigene BST-Implementierung (keine .NET-Standardklasse). Jeder besuchte Knoten wird mit einem `float`-Zeitstempel als Schlüssel eingefügt (`Insert`), wodurch die Besuchsreihenfolge strukturiert abgebildet wird. Unterstützt In-Order-Traversal zur sortierten Ausgabe aller besuchten Knoten.

*(Anmerkung: A\* ist im Code als `FindPathAStar` implementiert — Manhattan/Euklid-Heuristik via `hCost` — und kann im Inspector aktiviert werden.)*

---

## Visualisierung

- **Labyrinth:** Wand- und Boden-Prefabs werden zur Laufzeit instanziiert; Wandkacheln erhalten automatisch `Rigidbody2D` und `BoxCollider2D`.
- **Agenten:** Mehrere farbige Spieler-Objekte (rot/grün/blau) laufen gleichzeitig — je einer pro Algorithmus — und machen Unterschiede in Pfadwahl und Laufverhalten sichtbar.
- **Sackgassen:** Optional per `visualizeDeadEnds`-Toggle eingefärbt (`deadEndColor`), um die Labyrinthstruktur zu veranschaulichen.
- **Kamera:** Passt sich automatisch an die gesamten Renderer-Bounds des Labyrinths an (`CameraController`).

---

## Projektstruktur

```
Assets/
  Scripts/
    labyrinthGenerator.cs   – Grid-Erzeugung, DFS-Carving, Visualisierung, SpawnPlayers
    Pathfinder.cs           – Dijkstra, DFS, A*, FindDeadEnds, VisitTracker (BST)
    PlayerController.cs     – Agent-Bewegung, Algorithmuswahl per Enum, Coroutine-Steuerung
    Node.cs                 – Gitterzelle: position, TileType, gCost, hCost, parent
    BSTNode.cs              – Binärer Suchbaum (Insert, InOrder)
    TileData.cs             – isWall-Flag auf Tile-Prefabs
    CameraController.cs     – Auto-Fit nach Generierung
  Scenes/
    SampleScene.unity       – Hauptszene
  Prefabs/
    WallPrefab, PathPrefab, PlayerPrefab
```

---

## Starten und Ausführen

1. Unity **6000.3.8f1** öffnen
2. Projekt öffnen und Szene `Assets/Scenes/SampleScene.unity` laden
3. Im Inspector des `LabyrinthGenerator`-Objekts können `width`, `height`, `additionalPaths` und `visualizeDeadEnds` angepasst werden
4. **Play** drücken — Labyrinth wird generiert, Agenten starten automatisch

---

## Technische Hinweise

- Kein CLI-Build, kein Test-Runner — alle Iteration findet im Unity-Editor statt
- Der Code enthält keine Lambda-Ausdrücke und keine ternären Operatoren
- Dimensionen werden intern auf ungerade Werte gezwungen (DFS-Carving in 2-Zellen-Schritten)
- Pfadfindung nutzt echte Gitter-Adjazenz (`GetWalkableNeighbors`) über `nodeLookup`

---

## Prüfungsübersicht

### Implementierungsstand
| Anforderung | Status |
|---|---|
| Mindestens 3 Datenstrukturen | List, Stack, Dictionary, HashSet, BST (5+) |
| Mindestens 3 Algorithmen | Dijkstra, DFS, BST-Insert/InOrder (3+), A* optional |
| Visualisierung | Labyrinth, Agenten, Sackgassen-Einfärbung |


### Konfigurierbare Inspector-Felder
- `labyrinthGenerator`: `width`, `height`, `tileSize`, `additionalPaths`, `visualizeDeadEnds`, `deadEndColor`, `wallPrefab`, `floorPrefab`, `playerPrefab`
- `PlayerController`: `moveSpeed`, `algorithm` (Enum: Dijkstra / DFS / AStar)

### Diskussionspunkte (Präsentation)
- Warum Stack für Pfadumkehr statt Queue (LIFO vs. FIFO, Vorwärtsreihenfolge)
- Zeitkomplexität `List + Sort()` vs. `SortedList`/Heap für die Open-List
- Designentscheidung: 2D-Array als kanonischer Speicher + Dictionary für O(1)-Zugriff
- BST-VisitTracker: Nutzen der chronologischen Besuchsreihenfolge

