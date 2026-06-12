using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class labyrinthGenerator : MonoBehaviour
{
    [Header("Labyrinth")]
    public int width = 21;
    public int height = 21;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    private Node[,] grid; // 2D-Array zur Speicherung der Knoten
    private Dictionary<Vector2Int, Node> nodeLookup = new(); // Schnelle Abfrage von Knoten per Position
    public Vector2 tileSize = Vector2.one;  // Größe einer Kachel in Unity-Einheiten (z.B. 1x1 für 1 Einheit pro Kachel)
    public Vector2 tileOffset = Vector2.zero;   // Offset, um die Kacheln zentriert um den Ursprung zu platzieren
    public int additionalPaths = 500;   // Wie viele zusätzliche Pfade (durch zufälliges Entfernen von Wänden) nach der Hauptgenerierung erstellt werden

    [Header("Player")]
    public GameObject playerPrefab;

    [Header("Pfad-Visualisierung")]
    public bool visualizePath = true;
    private Pathfinder pathfinder;
    // Frei wählbare Farben: ein Weg wird per Dijkstra, der andere per DFS berechnet.
    public Color dijkstraPathColor = new Color(1f, 0.6f, 0.6f);   // Dijkstra-Weg
    public Color dfsPathColor = new Color(0.6f, 0.7f, 1f);   // DFS-Weg
    public Color aStarPathColor = new Color(0.6f, 0.7f, 1f);   // AStar-Weg

    public float pathStepDelay = 0.0f; // Verzögerung zwischen den Schritten der Pfad-Visualisierung (nur relevant, wenn visualizeAlgorithmSteps true ist)
    [Tooltip("Wie viele Kacheln pro Schritt eingefärbt werden. Höher = schneller. Min. 1.")]
    public int pathTilesPerFrame = 999;

    public Node StartNode { get; private set; }
    public Node GoalNode { get; private set; }
    public bool IsReady { get; private set; }
    private readonly Dictionary<PlayerController.PathfindingAlgorithm, PlayerController> spawnedPlayers = new(); // Gespawnte Spieler nach Algorithmus, um sie nach ihrer Pfad-Einfärbung freizugeben.

    // Richtungen für DFS: 2er-Schritte (oben, rechts, unten, links) um sicherzustellen, dass zwischen den Pfaden immer eine Wand bleibt, die wir entfernen können.
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 2),    // Oben
        new Vector2Int(2, 0),    // Rechts
        new Vector2Int(0, -2),   // Unten
        new Vector2Int(-2, 0)    // Links
    };

    public TextMeshProUGUI logText;

    private void Start()
    {
        pathfinder = GetComponent<Pathfinder>();
        // Validierung: Prefabs müssen zugewiesen sein
        if (wallPrefab == null || floorPrefab == null)
        {
            Debug.LogError("wallPrefab oder floorPrefab nicht im Inspector zugewiesen!");
            return;
        }

        if (width < 3 || height < 3)
        {
            Debug.LogError("width und height müssen mindestens 3 sein, damit ein Rand aus Wänden möglich ist!");
            return;
        }

        // Sicherstellen, dass Breite und Höhe ungerade sind, damit die Generierung korrekt funktioniert (wegen 2er-Schritten in DFS). Wenn gerade, um 1 erhöhen und Warnung ausgeben.
        if (width % 2 == 0)
        {
            width += 1;
            Debug.LogWarning($"width was even; incremented to {width} for proper maze generation.");
        }
        if (height % 2 == 0)
        {
            height += 1;
            Debug.LogWarning($"height was even; incremented to {height} for proper maze generation.");
        }

        Debug.Log($" Labyrinth wird generiert: {width}x{height}");
        GenerateGrid();
        GenerateLabyrinth();
        CarveAdditionalPaths();
        AddEntryAndExit();
        VisualizeLabyrinth();
        ColorWeights();
        IsReady = true;
        SpawnPlayers();
        if (visualizePath)
            StartCoroutine(RunPathVisualization());
        Debug.Log("Labyrinth fertig!");
    }

    // Erstellt das 2D-Array grid mit Node-Objekten, die alle als Wände initialisiert sind. Füllt auch das nodeLookup Dictionary für schnellen Zugriff.
    private void GenerateGrid()
    {
        grid = new Node[width, height];
        // Vor der Neuerstellung des Grids das nodeLookup Dictionary leeren, um veraltete Einträge zu entfernen.
        nodeLookup.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node newNode = new Node
                {
                    position = new Vector2Int(x, y),
                    type = TileType.Wall
                };
                grid[x, y] = newNode;
                nodeLookup[newNode.position] = newNode;
            }
        }
    }


    // Generiert das Labyrinth mit einem DFS-Algorithmus, der von der Startposition ausgeht. Es werden nur 2er-Schritte in den vier Hauptachsen gemacht, um sicherzustellen, dass zwischen den Pfaden immer eine Wand bleibt, die wir später entfernen können, um zusätzliche Pfade zu schaffen.
    private void GenerateLabyrinth()
    {
        Stack<Node> stack = new Stack<Node>();
        // Startknoten initialisieren: Position (1,1) (zweiter Knoten, da der Rand aus Wänden besteht) und als begehbar setzen. Dann auf den Stack legen.
        Vector2Int startPos = new Vector2Int(1, 1);
        Node startNode = grid[startPos.x, startPos.y];
        startNode.type = TileType.Floor;
        stack.Push(startNode);

        // Solange es Knoten auf dem Stack gibt, den aktuellen Knoten betrachten und nach unbesuchten Nachbarn in 2er-Schritten suchen. 
        // Wenn es welche gibt, zufällig einen auswählen, die Wand dazwischen entfernen, den Nachbarn als begehbar setzen und auf den Stack legen. Wenn es keine unbesuchten Nachbarn gibt, zurückgehen (Stack poppen) und von dort weitersuchen.
        while (stack.Count > 0)
        {
            // Aktuellen Knoten betrachten (ohne ihn zu entfernen)
            Node currentNode = stack.Peek();

            // Alle unbesuchten Nachbarn in 2er-Schritten sammeln
            List<Vector2Int> neighbors = new List<Vector2Int>();
            // Die Richtungen werden vor der Suche gemischt, damit die generierten Labyrinthe abwechslungsreicher aussehen.
            List<Vector2Int> dirs = ShuffleDirections();
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int nextPos = currentNode.position + dir;
                if (IsInBounds(nextPos) && grid[nextPos.x, nextPos.y].type == TileType.Wall)
                {
                    neighbors.Add(nextPos);
                }
            }

            if (neighbors.Count > 0)
            {
                // Zufällig einen unbesuchten Nachbarn auswählen und die Wand dazwischen entfernen
                Vector2Int nextPos = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int wallPos = currentNode.position + ((nextPos - currentNode.position) / 2);

                grid[wallPos.x, wallPos.y].type = TileType.Floor;
                grid[nextPos.x, nextPos.y].type = TileType.Floor;
                // Zufällige Gewichtung zwischen 1 und 20 für begehbare Knoten setzen, damit die Pfadfindungsalgorithmen unterschiedliche Wege bevorzugen.
                grid[nextPos.x, nextPos.y].weight = Random.Range(1, 20);

                stack.Push(grid[nextPos.x, nextPos.y]);
            }
            else
            {
                // Keine unbesuchten Nachbarn mehr, zurückgehen zum vorherigen Knoten
                stack.Pop();
            }
        }
    }

    // Erstellt zusätzliche Pfade, indem zufällig Wände entfernt werden, die mindestens 2 begehbare Nachbarn verbinden. Dadurch entstehen Schleifen und mehrere Lösungen im Labyrinth.
    private void CarveAdditionalPaths()
    {
        int pathsCarved = 0;
        int maxAttempts = additionalPaths * 30; // Sicherheitsmaßnahme, um eine Endlosschleife zu vermeiden, falls nicht genügend geeignete Wände gefunden werden.
        int attempts = 0;

        while (pathsCarved < additionalPaths && attempts < maxAttempts)
        {
            attempts++;

            // Eine zufällige Wand auswählen, die nicht am Rand liegt.
            int randomX = Random.Range(1, width - 1);
            int randomY = Random.Range(1, height - 1);

            // Falls die zufällig ausgewählte Kachel eine Wand ist, prüfen, ob sie mindestens 2 begehbare Nachbarn verbindet. Wenn ja, die Wand entfernen (in begehbar umwandeln) und den Zähler erhöhen.
            if (grid[randomX, randomY].type == TileType.Wall)
            {
                int floorNeighbors = 0;
                // Überprüfen der 4 Nachbarn (oben, rechts, unten, links) auf begehbare Kacheln. Jede begehbare Kachel erhöht die Anzahl der verbundenen Bereiche.
                if (randomX > 0 && grid[randomX - 1, randomY].type == TileType.Floor) floorNeighbors++;
                if (randomX < width - 1 && grid[randomX + 1, randomY].type == TileType.Floor) floorNeighbors++;
                if (randomY > 0 && grid[randomX, randomY - 1].type == TileType.Floor) floorNeighbors++;
                if (randomY < height - 1 && grid[randomX, randomY + 1].type == TileType.Floor) floorNeighbors++;

                // Nur Wände entfernen, die mindestens 2 begehbare Nachbarn verbinden, um sicherzustellen, dass tatsächlich ein zusätzlicher Pfad entsteht und keine Sackgasse.
                if (floorNeighbors >= 2)
                {
                    grid[randomX, randomY].type = TileType.Floor;
                    pathsCarved++;
                    Debug.Log($"Additional path {pathsCarved} carved at ({randomX}, {randomY})");
                }
            }
        }

        Debug.Log($"Carved {pathsCarved} additional paths for multiple solutions");
    }

    // Fügt einen festen Ein- und Ausgang hinzu.
    private void AddEntryAndExit()
    {
        // Fixer Eingang auf der linken Seite, zweite Kachel von unten (da der Rand aus Wänden besteht). Diese Position ist garantiert begehbar, da die DFS-Generierung auf (1,1) startet.
        grid[0, 1].type = TileType.Floor;
        // Fester Ausgang auf der rechten Seite, zweite Kachel von oben. 
        int exitY = height - 2;

        grid[width - 1, exitY].type = TileType.Floor;

        // StartNode und GoalNode setzen, damit die Pfadfindungsalgorithmen und Spieler ein Anfang und Ende definiert haben.
        StartNode = grid[0, 1];
        GoalNode = grid[width - 1, exitY];
    }

    // Gibt eine Liste der begehbaren Nachbarn eines Knotens zurück (nur orthogonal, keine Diagonalen).
    public List<Node> GetWalkableNeighbors(Node node)
    {
        List<Node> result = new List<Node>();
        // Wenn der übergebene Knoten null ist, wird eine leere Liste zurückgegeben.
        if (node == null) return result;

        // Die vier orthogonalen Richtungen definieren.
        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        // Für jede Richtung prüfen, ob es einen Knoten gibt und ob dieser begehbar ist. Wenn ja, wird er zur Ergebnisliste hinzugefügt.
        foreach (Vector2Int d in dirs)
        {
            if (nodeLookup.TryGetValue(node.position + d, out Node n) && n.type == TileType.Floor)
            {
                result.Add(n);
            }
        }
        return result;
    }

    // Gibt eine Liste aller begehbaren Knoten im Labyrinth zurück, um die Gewichtung sichtbar zu machen.
    public List<Node> GetFloorNodes()
    {
        List<Node> floors = new List<Node>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].type == TileType.Floor)
                {
                    floors.Add(grid[x, y]);
                }
            }
        }
        return floors;
    }

    // Hilfsmethode, um die Pfad-Visualisierung zu starten. Berechnet die Pfade für alle drei Algorithmen, weist sie den Spielern zu und färbt sie nacheinander
    private IEnumerator RunPathVisualization()
    {

        Pathfinder pathfinder = GetComponent<Pathfinder>();
        if (pathfinder == null)
        {
            pathfinder = gameObject.AddComponent<Pathfinder>();
        }
        // Berechnet die Pfade für alle drei Algorithmen. Die Pfade werden als Stacks von Nodes zurückgegeben, damit die Spieler sie in der richtigen Reihenfolge ablaufen können.
        Stack<Node> dijkstraPath = pathfinder.FindPathDijkstra(StartNode, GoalNode, this);
        Stack<Node> dfsPath = pathfinder.FindPathDFS(StartNode, GoalNode, this, out _);
        Stack<Node> aStarPath = pathfinder.FindPathAStar(StartNode, GoalNode, this);

        // Die gefundenen Pfade den Spielern zuweisen, damit sie wissen, welchen Pfad sie entlanglaufen sollen.
        spawnedPlayers[PlayerController.PathfindingAlgorithm.Dijkstra]
            .SetPath(new Stack<Node>(dijkstraPath));

        spawnedPlayers[PlayerController.PathfindingAlgorithm.AStar]
            .SetPath(new Stack<Node>(aStarPath));

        spawnedPlayers[PlayerController.PathfindingAlgorithm.DFS]
            .SetPath(new Stack<Node>(dfsPath));

        // Pro Algorithmus den Pfad Kachel für Kachel einfärben, mit optionaler Verzögerung zwischen den Schritten, damit man die Entwicklung des Pfades sehen kann.
        yield return StartCoroutine(TintPath(dfsPath, dfsPathColor));
        // Nachdem der Pfad eingefärbt ist, den entsprechenden Spieler freigeben, damit er loslaufen kann.
        ReleasePlayer(PlayerController.PathfindingAlgorithm.DFS);

        yield return StartCoroutine(TintPath(aStarPath, aStarPathColor));
        ReleasePlayer(PlayerController.PathfindingAlgorithm.AStar);

        yield return StartCoroutine(TintPath(dijkstraPath, dijkstraPathColor));
        ReleasePlayer(PlayerController.PathfindingAlgorithm.Dijkstra);

        Debug.Log($"Dijkstra-Weg: {dijkstraPath.Count} Felder, DFS-Weg: {dfsPath.Count} Felder, AStar-Weg: {aStarPath.Count} Felder eingefärbt.");
        // Die Anzahl der Felder in den Pfaden auch im UI-Text anzeigen.
        logText.text += $"Dijkstra-Weg: {dijkstraPath.Count} Felder\nDFS-Weg: {dfsPath.Count} Felder\nAStar-Weg: {aStarPath.Count} Felder";
    }


    // Gibt die Spielfigur mit dem angegebenen Algorithmus zum Loslaufen frei.
    private void ReleasePlayer(PlayerController.PathfindingAlgorithm algorithm)
    {
        // 
        if (spawnedPlayers.TryGetValue(algorithm, out PlayerController pc) && pc != null)
            pc.Release();
    }


    // Färbt einen Pfad Kachel für Kachel ein. Pro Schritt werden pathTilesPerFrame
    // Kacheln gefärbt (höher = schneller), danach wird einmal gewartet.
    private IEnumerator TintPath(Stack<Node> path, Color color)
    {
        int batch = Mathf.Max(1, pathTilesPerFrame);
        int count = 0;
        foreach (Node n in path)
        {
            TintNode(n, color);
            if (++count >= batch)
            {
                count = 0;
                yield return PathStepWait();
            }
        }
    }

    // Verzögert die Pfad-Visualisierung um die angegebene Zeit.
    private object PathStepWait()
    {
        if (pathStepDelay > 0f)
            return new WaitForSeconds(pathStepDelay);
        return null;
    }

    // Färbt eine einzelne Kachel entsprechend ihrer Node-Informationen ein.
    public void TintNode(Node node, Color color)
    {
        if (node == null || node.visual == null) return;
        SpriteRenderer sr = node.visual.GetComponent<SpriteRenderer>();
        if (sr == null) sr = node.visual.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }


    // Hilfsmethode, um die Weltposition einer Node zu berechnen, basierend auf ihrer Position im Grid, der Kachelgröße und dem Offset. Dadurch können die Spieler korrekt auf den Kacheln positioniert werden.
    public Vector3 GetWorldPosition(Node node)
    {
        return new Vector3(
            node.position.x * tileSize.x + tileOffset.x,
            node.position.y * tileSize.y + tileOffset.y,
            0f);
    }

    // Spawnt die Spieler am Startpunkt und weist ihnen ihre jeweiligen Pfade und Farben zu. Die Spieler warten mit dem Loslaufen, bis ihre Pfade eingefärbt sind, damit die Visualisierung vollständig sichtbar ist, bevor sie sich bewegen.
    private void SpawnPlayers()
    {
        Vector3 basePos = GetWorldPosition(StartNode);

        Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green };

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = basePos + new Vector3(i * tileSize.x * 1.2f, 0f, 0f);
            // GameObject-Instanz für den Spieler erstellen, benennen, taggen und positionieren.
            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            player.name = $"Player_{i + 1}";
            player.tag = "Player";
            player.transform.position = spawnPos;

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.AddComponent<SpriteRenderer>();

            if (player.GetComponent<Pathfinder>() == null)
                player.AddComponent<Pathfinder>();

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc == null)
                pc = player.AddComponent<PlayerController>();

            pc.SetGenerator(this);

            // Jedem Spieler seinen Algorithmus zuweisen, damit sie wissen, welchen Pfad sie entlanglaufen sollen. Die Zuordnung erfolgt in der Reihenfolge Dijkstra, AStar, DFS.
            switch (i)
            {
                case 0:
                    pc.algorithm = PlayerController.PathfindingAlgorithm.Dijkstra;
                    break;
                case 1:
                    pc.algorithm = PlayerController.PathfindingAlgorithm.AStar;
                    break;
                case 2:
                    pc.algorithm = PlayerController.PathfindingAlgorithm.DFS;
                    break;
            }

            // Die Farbe des Spielers entsprechend der vordefinierten Farben setzen.
            // Die Spieler werden etwas kleiner skaliert, damit sie besser auf die Kacheln passen.
            sr.color = playerColors[i];
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            // Bei aktiver Pfad-Visualisierung wartet der Spieler, bis seine Farbe
            // fertig gezeichnet ist; sonst läuft er sofort los.
            pc.waitForRelease = visualizePath;
            // Die Referenz zum Spieler in der spawnedPlayers-Dictionary speichern, damit wir ihn später freigeben können, wenn seine Pfad-Visualisierung abgeschlossen ist.
            spawnedPlayers[pc.algorithm] = pc;
        }
    }

    // Hilfsmethode zum Mischen der Richtungen, damit die generierten Labyrinthe abwechslungsreicher aussehen.
    private List<Vector2Int> ShuffleDirections()
    {
        List<Vector2Int> shuffled = new List<Vector2Int>(Directions);
        for (int i = 0; i < shuffled.Count; i++)
        {
            // Fisher-Yates Shuffle Algorithmus, um die Liste der Richtungen zu mischen.
            int randIndex = Random.Range(i, shuffled.Count);
            Vector2Int temp = shuffled[i];
            shuffled[i] = shuffled[randIndex];
            shuffled[randIndex] = temp;
        }
        return shuffled;
    }

    // Überprüft, ob eine Position innerhalb der Grenzen des Grids liegt (unter Berücksichtigung des Wand-Rands).
    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1;
    }

    // Visualisiert das Labyrint via Instanziierung von Boden- und Wand-Prefabs entsprechend der Informationen in den Nodes.
    private void VisualizeLabyrinth()
    {
        int wallCount = 0, floorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                GameObject prefab = floorPrefab;
                if (node.type == TileType.Wall)
                {
                    prefab = wallPrefab;
                }

                float px = x * tileSize.x + tileOffset.x;
                float py = y * tileSize.y + tileOffset.y;
                Vector3 position = new Vector3(px, py, 0f);

                // 
                GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}_{node.type}";
                node.visual = tile;

                // Tile wird auf tileSize skaliert
                tile.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);
                bool blockPlayer = node.type == TileType.Wall;
                if (blockPlayer)
                {
                    wallCount++;
                }
                else
                {
                    floorCount++;
                }
            }
        }

        Debug.Log($" Kacheln erstellt: {wallCount} Wände, {floorCount} Böden");
    }

    // Färbt begehbare Kacheln je nach ihren Betretungskosten ein
    private void ColorWeights()
    {
        foreach (Node node in GetFloorNodes())
        {
            SpriteRenderer sr = node.visual.GetComponent<SpriteRenderer>();

            if (sr != null) sr.color = GetWeightColor(node.weight);
        }
    }

    // Gibt eine Farbe zurück, die die Betretungskosten einer Kachel repräsentiert. Niedrigere Kosten = hellere Farbe, höhere Kosten = dunklere Farbe.
    private Color GetWeightColor(float weight)
    {
        if (weight <= 5)
            return new Color(0.85f, 0.75f, 0.60f);

        if (weight <= 10)
            return new Color(0.70f, 0.55f, 0.35f);

        if (weight <= 15)
            return new Color(0.50f, 0.35f, 0.20f);

        return new Color(0.25f, 0.15f, 0.05f);
    }
}