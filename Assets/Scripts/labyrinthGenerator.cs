using UnityEngine;
using System.Collections.Generic;

public class labyrinthGenerator : MonoBehaviour
{
    [Header("Labyrinth")]
    public int width = 21;
    public int height = 21;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public Vector2 tileSize = Vector2.one;
    public Vector2 tileOffset = Vector2.zero;
    // Number of additional paths to carve through the maze (0 = single path, higher = more paths)
    public int additionalPaths = 5;

    [Header("Player")]
    public GameObject playerPrefab;

    [Header("Sackgassen-Visualisierung")]
    // Wenn aktiv, werden nach der Generierung alle echten Sackgassen rot eingefärbt:
    // ein Feld mit nur einem begehbaren Nachbarn (Grad 1) plus der Korridor
    // bis zur nächsten Kreuzung.
    public bool visualizeDeadEnds = false;
    public Color deadEndColor = new Color(1f, 0.45f, 0.45f);  // Sackgassen (rot)

    public Node StartNode { get; private set; }
    public Node GoalNode  { get; private set; }
    public bool IsReady   { get; private set; }

    private Node[,] grid; // 2D-Array zur Speicherung
    private Dictionary<Vector2Int, Node> nodeLookup = new(); // Schnelle Abfrage

    // Richtungen für DFS: 2er-Schritte (oben, rechts, unten, links)
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 2),    // Oben
        new Vector2Int(2, 0),    // Rechts
        new Vector2Int(0, -2),   // Unten
        new Vector2Int(-2, 0)    // Links
    };
    
    private void Start()
    {
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

        // Ensure odd dimensions so carving with 2-step directions yields a single-tile outer wall.
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
        IsReady = true;
        // Position UI legend above the generated labyrinth (if assigned)
        if (visualizeDeadEnds)
        {
            RunDeadEndVisualization();
        }

        SpawnPlayers();
        Debug.Log("Labyrinth fertig!");
    }

    private void GenerateGrid()
    {
        grid = new Node[width, height];
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

    private void GenerateLabyrinth()
    {
        // Iterative DFS: use Peek so we explore deep paths and backtrack correctly
        Stack<Node> stack = new Stack<Node>();
        Vector2Int startPos = new Vector2Int(1, 1);
        Node startNode = grid[startPos.x, startPos.y];
        startNode.type = TileType.Floor;
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            Node currentNode = stack.Peek();

            // Collect valid unvisited neighbors (2-cell steps)
            List<Vector2Int> neighbors = new List<Vector2Int>();
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
                // Choose random neighbor and carve path
                Vector2Int nextPos = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int wallPos = currentNode.position + ((nextPos - currentNode.position) / 2);

                grid[wallPos.x, wallPos.y].type = TileType.Floor;
                grid[nextPos.x, nextPos.y].type = TileType.Floor;

                stack.Push(grid[nextPos.x, nextPos.y]);
            }
            else
            {
                // No unvisited neighbors -> backtrack
                stack.Pop();
            }
        }
    }

    private void CarveAdditionalPaths()
    {
        // Create additional paths by randomly carving through walls
        // This creates loops and multiple solutions to the maze
        int pathsCarved = 0;
        int maxAttempts = additionalPaths * 20; // Prevent infinite loops
        int attempts = 0;

        while (pathsCarved < additionalPaths && attempts < maxAttempts)
        {
            attempts++;

            // Pick a random wall position
            int randomX = Random.Range(1, width - 1);
            int randomY = Random.Range(1, height - 1);

            if (grid[randomX, randomY].type == TileType.Wall)
            {
                // Check if this wall separates two floor tiles
                int floorNeighbors = 0;
                if (randomX > 0 && grid[randomX - 1, randomY].type == TileType.Floor) floorNeighbors++;
                if (randomX < width - 1 && grid[randomX + 1, randomY].type == TileType.Floor) floorNeighbors++;
                if (randomY > 0 && grid[randomX, randomY - 1].type == TileType.Floor) floorNeighbors++;
                if (randomY < height - 1 && grid[randomX, randomY + 1].type == TileType.Floor) floorNeighbors++;

                // Only carve if it connects at least 2 different floor areas
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

    private void AddEntryAndExit()
    {
        // Fixed entry on the left side next to the start area.
        grid[0, 1].type = TileType.Floor;

        // Prefer an exit on the right side that touches an existing floor tile.
        List<int> exitCandidates = new List<int>();
        for (int y = 1; y < height - 1; y++)
        {
            if (grid[width - 2, y].type == TileType.Floor)
            {
                exitCandidates.Add(y);
            }
        }

        int exitY = (exitCandidates.Count > 0)
            ? exitCandidates[Random.Range(0, exitCandidates.Count)]
            : height - 2;

        grid[width - 1, exitY].type = TileType.Floor;

        StartNode = grid[0, 1];
        GoalNode  = grid[width - 1, exitY];
    }

    /// <summary>
    /// Liefert die begehbaren (Floor) Nachbarn eines Knotens in 1-Schritt-Distanz.
    /// Nutzt das Dictionary nodeLookup für O(1)-Zugriff.
    /// </summary>
    public List<Node> GetWalkableNeighbors(Node node)
    {
        List<Node> result = new List<Node>();
        if (node == null) return result;

        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        foreach (Vector2Int d in dirs)
        {
            if (nodeLookup.TryGetValue(node.position + d, out Node n) && n.type == TileType.Floor)
            {
                result.Add(n);
            }
        }
        return result;
    }

    public List<Node> GetFloorNodes()
    {
        List<Node> floors = new List<Node>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].type == TileType.Floor)
                    floors.Add(grid[x, y]);
        return floors;
    }

    /// <summary>
    /// Ermittelt alle echten Sackgassen-Korridore und färbt sie ein.
    /// </summary>
    private void RunDeadEndVisualization()
    {
        Pathfinder pathfinder = GetComponent<Pathfinder>();
        if (pathfinder == null)
            pathfinder = gameObject.AddComponent<Pathfinder>();

        List<Node> deadEndCells = pathfinder.FindDeadEnds(this);

        foreach (Node n in deadEndCells)
            TintNode(n, deadEndColor);

        Debug.Log($"Sackgassen: {deadEndCells.Count} Felder markiert.");
    }

    private void TintNode(Node node, Color color)
    {
        if (node == null || node.visual == null) return;
        SpriteRenderer sr = node.visual.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    public Vector3 GetWorldPosition(Node node)
    {
        return new Vector3(
            node.position.x * tileSize.x + tileOffset.x,
            node.position.y * tileSize.y + tileOffset.y,
            0f);
    }

    private void SpawnPlayers()
    {
        Vector3 basePos = GetWorldPosition(StartNode);

        Color[] colors = new Color[] { Color.red, Color.green, Color.blue };

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = basePos + new Vector3(i * tileSize.x * 1.2f, 0f, 0f);

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

            // Assign algorithm and visual properties after components are added so Awake() won't override them
            switch (i)
            {
                case 0:
                    pc.algorithm = PlayerController.PathfindingAlgorithm.Dijkstra;
                    break;
                case 1:
                    pc.algorithm = PlayerController.PathfindingAlgorithm.DFS;
                    break;
            }

            sr.color = colors[i];
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
    }

    
    private List<Vector2Int> ShuffleDirections()
    {
        List<Vector2Int> shuffled = new List<Vector2Int>(Directions);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randIndex = Random.Range(i, shuffled.Count);
            Vector2Int temp = shuffled[i];
            shuffled[i] = shuffled[randIndex];
            shuffled[randIndex] = temp;
        }
        return shuffled;
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x > 0 && pos.x < width - 1 && pos.y > 0 && pos.y < height - 1;
    }

    /// <summary>
    /// Visualizes the grid using the configured prefabs, tileSize and tileOffset.
    /// </summary>
    private void VisualizeLabyrinth()
    {
        int wallCount = 0, floorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                GameObject prefab = floorPrefab;
                if (node.type == TileType.Wall) {
                    prefab = wallPrefab;
                }

                float px = x * tileSize.x + tileOffset.x;
                float py = y * tileSize.y + tileOffset.y;
                Vector3 position = new Vector3(px, py, 0f);

                GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}_{node.type}";
                node.visual = tile;

                // Apply tile size as local scale so prefabs fit the grid
                tile.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);

                bool blockPlayer = tile.GetComponent<TileData>()?.isWall ?? (node.type == TileType.Wall);
                if (blockPlayer)
                {
                    wallCount++;

                    Rigidbody2D rb2d = tile.GetComponent<Rigidbody2D>();
                    if (rb2d == null)
                    {
                        rb2d = tile.AddComponent<Rigidbody2D>();
                    }
                    rb2d.bodyType = RigidbodyType2D.Kinematic;
                    rb2d.gravityScale = 0f;
                    rb2d.constraints = RigidbodyConstraints2D.FreezeAll;

                    // Ensure a collider exists for wall collisions
                    Collider2D col = tile.GetComponent<Collider2D>();
                    if (col == null)
                    {
                        tile.AddComponent<BoxCollider2D>();
                    }
                }
                else
                {
                    floorCount++;
                }
            }
        }

        Debug.Log($" Kacheln erstellt: {wallCount} Wände, {floorCount} Böden");
    }}