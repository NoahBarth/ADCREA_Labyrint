using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour
{
    /// <summary>
    /// Dijkstra-Algorithmus zur Pfadfindung
    /// </summary>
    public Stack<Node> FindPath(Node start, Node goal, List<Node> allNodes)
    {
        if (start == null || goal == null)
        {
            Debug.LogError("Start oder Goal ist null!");
            return new Stack<Node>();
        }

        HashSet<Node> closedSet = new HashSet<Node>(); // Verarbeitete Knoten
        List<Node> openList = new List<Node>(); // Zu untersuchende Knoten

        // Initialisierung
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.parent = null;
        }

        start.gCost = 0;
        openList.Add(start);

        while (openList.Count > 0)
        {
            // FEHLER BEHOBEN: openList.Sort() + openList[0] statt openList
            openList.Sort();
            Node current = openList[0];
            openList.RemoveAt(0);

            if (current == goal)
            {
                break; // Pfad gefunden!
            }

            closedSet.Add(current);

            // Nachbarn evaluieren (vereinfacht: alle Knoten)
            foreach (Node neighbor in allNodes)
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

    /// <summary>
    /// Backtracking: Pfad vom Ziel zum Start zurückverfolgen und umkehren
    /// </summary>
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

    public Stack<Node> FindPathAStar(Node start, Node goal, List<Node> allNodes)
    {
        if (start == null || goal == null)
        {
            Debug.LogError("Start oder Goal ist null!");
            return new Stack<Node>();
        }

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

            foreach (Node neighbor in allNodes)
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
