using UnityEngine;
using System.Collections.Generic;

public class labyrinthGenerator : MonoBehaviour
{
    [SerializeField] public int width = 21;
    [SerializeField] public int height = 21;
    [SerializeField] public GameObject wallPrefab;
    [SerializeField] public GameObject floorPrefab;

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
        GenerateGrid();
        ApplyDFSGeneration(Vector2Int.one); // Startpunkt (1,1)
        Visualize();
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

    /// <summary>
    /// DFS-Algorithmus zur automatischen Labyrinth-Generierung
    /// Entfernt Wände und erzeugt einen Pfad
    /// </summary>
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

    /// <summary>
    /// Findet alle unbesuchten Nachbarn (2er-Schritte für richtige Labyrinth-Struktur)
    /// </summary>
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

    /// <summary>
    /// Prüft, ob eine Position innerhalb des Grids liegt
    /// </summary>
    private bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    /// <summary>
    /// Erzeugt die visuellen GameObjects basierend auf dem Grid
    /// RICHTIG: Mit verschachtelten for-Loops statt foreach
    /// </summary>
    private void Visualize()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                GameObject prefab = (node.type == TileType.Wall) ? wallPrefab : floorPrefab;
                
                Vector3 position = new Vector3(x, 0, y);
                GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
                node.visual = tile;
            }
        }
    }
}