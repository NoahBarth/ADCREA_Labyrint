using UnityEngine;
using System.Collections.Generic;

public class labyrinthGenerator : MonoBehaviour
{
    public int width = 21;
    public int height = 21;
    public GameObject wallPrefab;
    public GameObject floorPrefab;

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

        Debug.Log($" Labyrinth wird generiert: {width}x{height}");
        GenerateGrid();
        ApplyDFSGeneration(Vector2Int.one); // Startpunkt (1,1)
        Visualize();
        Debug.Log("Labyrinth fertig!");
    }

    /// <summary>
    /// Erzeugt das initiale Grid - alle Kacheln als Wände
    /// </summary>
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

    /// DFS-Algorithmus zur automatischen Labyrinth-Generierung
    /// Entfernt Wände und erzeugt einen Pfad
    private void ApplyDFSGeneration(Vector2Int startPos)
    {
        if (!IsInside(startPos))
        {
            Debug.LogError("Startposition außerhalb des Grids!");
            return;
        }

        Stack<Node> stack = new Stack<Node>(); // Stack für das Backtracking
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Node startNode = nodeLookup[startPos];
        startNode.type = TileType.Floor;
        visited.Add(startPos);
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            Node current = stack.Peek();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current.position, visited);

            if (unvisitedNeighbors.Count > 0)
            {
                // Zufälligen unbesuchten Nachbarn wählen
                Vector2Int nextPos = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                
                // Wand zwischen aktuellem Knoten und Nachbar entfernen
                Vector2Int wallBetween = current.position + ((nextPos - current.position) / 2);
                
                nodeLookup[wallBetween].type = TileType.Floor;
                nodeLookup[nextPos].type = TileType.Floor;

                visited.Add(nextPos);
                stack.Push(nodeLookup[nextPos]);
            }
            else
            {
                // Backtrack: Keine unbesuchten Nachbarn, zurück im Stack
                stack.Pop();
            }
        }
    }

    /// Findet alle unbesuchten Nachbarn (2er-Schritte für richtige Labyrinth-Struktur)
    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int pos, HashSet<Vector2Int> visited)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int direction in Directions)
        {
            Vector2Int neighborPos = pos + direction;

            if (IsInside(neighborPos) && !visited.Contains(neighborPos))
            {
                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

 
    /// Prüft, ob eine Position innerhalb des Grids liegt
    private bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }


    /// Erzeugt die visuellen GameObjects basierend auf dem Grid (2D Version)

    private void Visualize()
    {
        int wallCount = 0, floorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                GameObject prefab = (node.type == TileType.Wall) ? wallPrefab : floorPrefab;
                
                // 2D Position: x und y als normale Koordinaten, z=0 für 2D
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}_{node.type}";
                node.visual = tile;

                if (node.type == TileType.Wall)
                {
                    wallCount++;
                    
                    // Rigidbody2D für Wände hinzufügen (Collision für Player)
                    Rigidbody2D rb2d = tile.GetComponent<Rigidbody2D>();
                    if (rb2d == null)
                    {
                        rb2d = tile.AddComponent<Rigidbody2D>();
                    }
                    
                    // Rigidbody2D konfigurieren: Kinematisch, damit Wand nicht fällt
                    rb2d.isKinematic = true;
                    rb2d.gravityScale = 0;
                    rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
                }
                else
                {
                    floorCount++;
                }
            }
        }

        Debug.Log($" Kacheln erstellt: {wallCount} Wände, {floorCount} Böden");
    }
}