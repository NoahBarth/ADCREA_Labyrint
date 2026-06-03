# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 (6000.3.8f1) 2D top-down maze solver for the HSLU module **ADCREA** (Algorithmen & Datenstrukturen / Creative Computing). The player navigates automatically through a procedurally generated labyrinth using pathfinding algorithms. Built by Sandro Fankhauser and Noah Barth.

## Anforderungen (Requirements)

### Datenstrukturen
- **2D Array**: Zur Speicherung und Initialisierung aller Bodenkacheln.
- **List**: Resultat der Pfadsuche und Backtracking.
- **Stack**: Zur Speicherung der berechneten Route, damit der Spieler die Wegpunkte nacheinander abarbeiten kann. (Dijkstra/A* liefern den Pfad in Rückwärts-Reihenfolge; ein Stack dreht ihn beim `Pop()` in die vom Spieler benötigte Vorwärts-Reihenfolge. Eine Queue wäre hier falsch — siehe Dozenten-Feedback.)
- **Dictionary**: Für eine schnelle Koordinatenabfrage (Vector2 zu Node), um effizient zwischen Wänden und begehbaren Wegen zu unterscheiden.

### Algorithmen
- **Dijkstra oder A* & SortedList & HashSet**: Als Standard-Algorithmus, um zuverlässig den kürzesten Weg zum Ausgang zu finden.
- **DFS (Depth-First Search)**: Um alternative Pfade zu visualisieren oder Sackgassen im Labyrinth aufzuzeigen.
- **BST (Binary Search Tree)**: Jeder besuchte Knoten wird inklusive Zeitstempel eingefügt, um die Besuchsreihenfolge strukturiert abzubilden.

## Building and Running

Open the project in **Unity 6000.3.8f1** and press Play in the Editor. There is no CLI build step or test runner — all iteration happens inside the Unity Editor. The main scene is `Assets/Scenes/SampleScene.unity`; a prefab-based variant lives at `Assets/LabyrinthPrefab.unity`.

## Architecture

All game logic lives in four scripts under [Assets/Scripts/](Assets/Scripts/).

### Data model — [Node.cs](Assets/Scripts/Node.cs)
Single grid cell. Holds `Vector2Int position`, `TileType` (Wall / Floor), a reference to its spawned `GameObject`, and the pathfinding state fields `gCost`, `hCost`, and `parent`. Implements `IComparable<Node>` so nodes sort by f-cost (`gCost + hCost`), which the open-list `Sort()` calls rely on.

### Maze generation — [labyrinthGenerator.cs](Assets/Scripts/labyrinthGenerator.cs)
Runs on `Start`. Pipeline:
1. `GenerateGrid()` — allocates `Node[width, height]` (2D array, primary structure) and fills `Dictionary<Vector2Int, Node> nodeLookup` for O(1) position lookup.
2. `GenerateLabyrinth()` — iterative DFS (recursive-backtracker) using a `Stack<Node>`, carving paths in 2-cell steps. Dimensions are forced odd so a single-tile outer wall is always preserved.
3. `AddEntryAndExit()` — entry fixed at `(0, 1)`; exit chosen randomly on the right wall adjacent to an existing floor tile.
4. `VisualizeLabyrinth()` — instantiates wall/floor prefabs; adds `Rigidbody2D` (Kinematic, gravity 0) and `BoxCollider2D` to wall tiles at runtime.

Inspector-configurable: `width`, `height`, `wallPrefab`, `floorPrefab`, `tileSize`, `tileOffset`.

### Pathfinding — [Pathfinder.cs](Assets/Scripts/Pathfinder.cs)
Two public methods, both return a `Stack<Node>` in **forward order** (start → goal):

- `FindPath(start, goal, allNodes)` — Dijkstra
- `FindPathAStar(start, goal, allNodes)` — A* (adds Manhattan/Euclidean heuristic via `hCost`)

Both use `HashSet<Node> closedSet` and `List<Node> openList` sorted via `openList.Sort()` each iteration. Backtracking is handled by `BacktrackPath`: walks `parent` pointers from goal → start into a `List<Node>`, then pushes each node onto a `Stack<Node>` to reverse it into forward order.

A third method `FindPathDFS(start, goal, generator, out deadEndNodes)` implements iterative **DFS** with a `Stack<Node>` plus a `cameFrom` dictionary, returning the path as a forward-order `Stack<Node>`. All three pathfinders use real grid adjacency via `labyrinthGenerator.GetWalkableNeighbors(node)` — the 1-step orthogonal floor neighbors looked up in `nodeLookup`.

`FindDeadEnds(generator)` is the dead-end detector used for visualization: a floor cell with exactly one walkable neighbor (degree 1, excluding start/goal) is a dead-end tip; the corridor is then traced from that tip until the next junction (degree ≥ 3). When the generator's `visualizeDeadEnds` inspector toggle is on, those corridors are colored `deadEndColor` after generation (see `RunDeadEndVisualization`/`TintNode`).

Also contains `VisitTracker` (backed by a custom `BSTNode` BST keyed on `float` timestamps) used to record node visit order chronologically.

### Camera — [CameraController.cs](Assets/Scripts/CameraController.cs)
Waits one frame via coroutine after `Start` (so generation completes) then auto-fits the orthographic main camera to encapsulate all renderer bounds under the labyrinth root, with configurable padding.

## Key Design Decisions

- **2D array + Dictionary**: `grid[x,y]` is the canonical store; `nodeLookup` provides position-keyed access without iteration.
- **Stack for the path result**: Dijkstra/A* reconstruct the path backwards; pushing onto a Stack naturally reverses it so the player can `Pop()` waypoints in forward order.
- **List + Sort() for open list**: fulfills the assignment requirement; a `SortedSet` or `SortedList` would be O(log n) per insertion instead of O(n log n) per sort and is the preferred upgrade if performance matters.
- **Odd dimensions enforced**: the DFS carves in 2-cell steps, so even-sized grids break the outer wall — the generator increments width/height automatically and logs a warning.
