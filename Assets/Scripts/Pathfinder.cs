using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour
{
    /// Dijkstra-Algorithmus zur Pfadfindung
    public Stack<Node> FindPath(Node start, Node goal, labyrinthGenerator generator) //geringsten Gesamtkosten gCost
    {
        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("Start, Goal oder Generator ist null!");
            return new Stack<Node>();
        }

        List<Node> allNodes = generator.GetFloorNodes();
        HashSet<Node> closedSet = new HashSet<Node>(); // Verarbeitete Knoten
        List<Node> openList = new List<Node>(); // Zu untersuchende Knoten

        // Initialisierung
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.parent = null;
        }

        start.gCost = 0; //geringsten Kosten zum Start
        openList.Add(start);

        while (openList.Count > 0) // Solange es Knoten gibt, die noch untersucht werden müssen
        {

            openList.Sort();
            Node current = openList[0];
            openList.RemoveAt(0);

            if (current == goal)//geringste Kosten wird genommen
            {
                break; // Pfad gefunden!
            }

            closedSet.Add(current);

            // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = current.gCost + Vector2Int.Distance(current.position, neighbor.position);

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.gCost = tentativeGCost;
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        // FEHLER BEHOBEN: Backtracking-Logik in die Methode verlegt
        Stack<Node> finalStack = BacktrackPath(goal);
        return finalStack;
    }

   
    /// Backtracking: Pfad vom Ziel zum Start zurückverfolgen und umkehren

    private Stack<Node> BacktrackPath(Node goal)
    {
        List<Node> pathList = new List<Node>();
        Node temp = goal;

        while (temp != null)
        {
            pathList.Add(temp);
            temp = temp.parent;
        }

        Stack<Node> finalStack = new Stack<Node>();
        foreach (var node in pathList)
        {
            finalStack.Push(node); // Umkehrung für Vorwärts-Reihenfolge
        }

        return finalStack;
    }

 
    /// A*-Algorithmus (Dijkstra + Heuristik)

    public Stack<Node> FindPathAStar(Node start, Node goal, labyrinthGenerator generator)
    {
        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("Start, Goal oder Generator ist null!");
            return new Stack<Node>();
        }

        List<Node> allNodes = generator.GetFloorNodes();
        HashSet<Node> closedSet = new HashSet<Node>();
        List<Node> openList = new List<Node>();

        // Initialisierung
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.hCost = Vector2Int.Distance(node.position, goal.position);
            node.parent = null;
        }

        start.gCost = 0;
        openList.Add(start);

        while (openList.Count > 0)
        {
            openList.Sort(); // Sortiert nach gCost + hCost (F-Kosten)
            Node current = openList[0];
            openList.RemoveAt(0);

            if (current == goal)
            {
                break;
            }

            closedSet.Add(current);

            // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = current.gCost + Vector2Int.Distance(current.position, neighbor.position);

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.gCost = tentativeGCost;
                    neighbor.parent = current;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        return BacktrackPath(goal);
    }

    /// <summary>
    /// DFS (Depth-First Search) zur Pfadfindung. Im Gegensatz zu Dijkstra/A*
    /// findet DFS nicht zwingend den kürzesten Weg, eignet sich aber, um
    /// alternative Pfade und Sackgassen im Labyrinth aufzuzeigen.
    /// Gibt den gefundenen Pfad als Stack in Vorwärts-Reihenfolge (Start -> Ziel)
    /// zurück und liefert über deadEndNodes alle besuchten Knoten, die NICHT
    /// auf dem finalen Pfad liegen (= Sackgassen / alternative Wege).
    /// </summary>
    public Stack<Node> FindPathDFS(Node start, Node goal, labyrinthGenerator generator, out List<Node> deadEndNodes)
    {
        deadEndNodes = new List<Node>();
        Stack<Node> finalPath = new Stack<Node>();

        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("DFS: Start, Goal oder Generator ist null!");
            return finalPath;
        }

        HashSet<Node> visited = new HashSet<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        Stack<Node> stack = new Stack<Node>();

        stack.Push(start);
        visited.Add(start);
        cameFrom[start] = null;

        bool found = false;

        while (stack.Count > 0)
        {
            Node current = stack.Pop();

            if (current == goal)
            {
                found = true;
                break;
            }

            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    stack.Push(neighbor);
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("DFS: Kein Pfad zum Ziel gefunden!");
            return finalPath;
        }

        // Pfad rekonstruieren (Ziel -> Start) und Knoten auf dem Pfad merken.
        HashSet<Node> pathSet = new HashSet<Node>();
        Node temp = goal;
        while (temp != null)
        {
            finalPath.Push(temp); // Push dreht Ziel->Start in Start->Ziel (Vorwärts-Reihenfolge)
            pathSet.Add(temp);
            cameFrom.TryGetValue(temp, out temp);
        }

        // Sackgassen / alternative Wege = besuchte Knoten, die nicht auf dem Pfad liegen.
        foreach (Node n in visited)
        {
            if (!pathSet.Contains(n))
                deadEndNodes.Add(n);
        }

        return finalPath;
    }
}

/// <summary>
/// Binary Search Tree für Zeitstempel-basierte Besuchsreihenfolge
/// </summary>
public class BSTNode
{
    public float timestamp;
    public Node labyrinthNode;
    public BSTNode left;
    public BSTNode right;

    public BSTNode(float time, Node node)
    {
        timestamp = time;
        labyrinthNode = node;
        left = null;
        right = null;
    }
}

/// <summary>
/// VisitTracker verwaltet die Besuchsreihenfolge mit Zeitstempeln
/// </summary>
public class VisitTracker
{
    private BSTNode root;

    public void Insert(float time, Node node)
    {
        root = InsertRecursive(root, time, node);
    }

    private BSTNode InsertRecursive(BSTNode current, float time, Node node)
    {
        if (current == null)
        {
            return new BSTNode(time, node);
        }

        if (time < current.timestamp)
        {
            current.left = InsertRecursive(current.left, time, node);
        }
        else
        {
            current.right = InsertRecursive(current.right, time, node);
        }

        return current;
    }

    public void InorderTraversal(BSTNode node)
    {
        if (node == null) return;

        InorderTraversal(node.left);
        Debug.Log($"Zeit: {node.timestamp}, Position: {node.labyrinthNode.position}");
        InorderTraversal(node.right);
    }
}
