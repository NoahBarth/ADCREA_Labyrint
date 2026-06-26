using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Pathfinder : MonoBehaviour
{
    /// Dijkstra-Algorithmus zur Pfadfindung    
    public Stack<Node> FindPathDijkstra(Node start, Node goal, labyrinthGenerator generator) //geringsten Gesamtkosten gCost
    {
        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("Start, Goal oder Generator ist null!");
            return new Stack<Node>();
        }
        int visitedDijkstraNodes = 0;

        List<Node> allNodes = generator.GetFloorNodes();
        HashSet<Node> closedSet = new HashSet<Node>(); // Verarbeitete Knoten
        List<Node> openList = new List<Node>(); // Zu untersuchende Knoten

        // Initialisierung
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.hCost = 0; // Dijkstra ignoriert Heuristik, aber Feld muss initialisiert werden
            node.parent = null;
        }

        start.gCost = 0; //geringsten Kosten zum Start
        openList.Add(start);

        while (openList.Count > 0) // Solange es Knoten gibt, die noch untersucht werden müssen
        {

            // Sortiert ohne Lambda über Node.CompareTo (vergleicht nach fCost).
            // Bei Dijkstra ist hCost = 0, also wird effektiv nach gCost sortiert.
            openList.Sort();
            Node current = openList[0];
            openList.RemoveAt(0);
            visitedDijkstraNodes++;

            if (current == goal)//geringste Kosten wird genommen
            {
                break; // Pfad gefunden!
            }

            closedSet.Add(current);
            // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Betretungskosten der Zelle als Kantengewicht -> Dijkstra weicht von der reinen
                // Schritt-Zählung (DFS) ab und wählt dadurch einen anderen Weg.
                float tentativeGCost = current.gCost + neighbor.weight;

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

        Stack<Node> finalStack = BacktrackPath(goal);
        Debug.Log($"Dijkstra: {visitedDijkstraNodes} Knoten untersucht");
        if (generator.logText != null)
        {
            generator.logText.text += $"Dijkstra: {visitedDijkstraNodes} Knoten untersucht\n";
        }
        return finalStack;
    }


    /// Backtracking Pfad vom Ziel zum Start zurückverfolgen und umkehren damit wir den stack umkehren um in vorwärtsreihenfolge zu bekommmen

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
    /// damit nicht nur der kürzeste Pfad gefunden wird, sondern auch schneller, indem die Suche in Richtung Ziel gelenkt wird.

    public Stack<Node> FindPathAStar(Node start, Node goal, labyrinthGenerator generator)
    {
        int visitedAStarNodes = 0;
        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("Start, Goal oder Generator ist null!");
            return new Stack<Node>();
        }

        List<Node> allNodes = generator.GetFloorNodes();
        HashSet<Node> closedSet = new HashSet<Node>();
        List<Node> openList = new List<Node>();

        // Initialisierung der gCost
        foreach (Node node in allNodes)
        {
            node.gCost = float.MaxValue;
            node.hCost = Mathf.Abs(node.position.x - goal.position.x) + Mathf.Abs(node.position.y - goal.position.y); // Manhatten Distance
            node.parent = null;
        }

        start.gCost = 0;
        openList.Add(start);

        while (openList.Count > 0)
        {
            //hauptschleife: Knoten mit niedrigsten fCost (gCost + hCost) wird ausgewählt & list sortiert, damit der Knoten mit den geringsten Kosten immer an erster Stelle steht
            openList.Sort();
            Node current = openList[0];
            openList.RemoveAt(0);
            visitedAStarNodes++;

            if (current == goal)//wenn ziel erreicht
            {
                break;
            }

            closedSet.Add(current);

            // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = current.gCost + neighbor.weight;

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
        Debug.Log($"A*: {visitedAStarNodes} Knoten untersucht");
        if (generator.logText != null)
        {
            generator.logText.text += $"A*: {visitedAStarNodes} Knoten untersucht\n";
        }
        return BacktrackPath(goal);
    }


    // DFS pathfinder, gibt nicht zweingend kprzesten Weg aber zeigt alternative Pfade
    //gibt gefundenen Pfad als Stack in vorwärtsreihenfolge zurück
    public Stack<Node> FindPathDFS(Node start, Node goal, labyrinthGenerator generator)
    {
        int visitedDFSNodes = 0; // visited nodes zählen, um das tiefste zu finden
        Stack<Node> finalPath = new Stack<Node>();

        if (start == null || goal == null || generator == null) // überprüft ob start, ziel oder generator null ist, wird ein fehler ausgegeben, weil der algo nicht funktionieren würde
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
        // solange es knoten gibt, die noch untersucht werden müssen, wird der oberste Knoten vom Stack genommen und überprüft ob das Ziel erreicht ist.  Weil dfs tiefenorientiert untersucht es den untersten knoten
        while (stack.Count > 0)
        {
            Node current = stack.Pop();
            visitedDFSNodes++;

            if (current == goal)
            {
                found = true;
                break;
            }

            // überprüft begehbare felder um aktuellen knoten wenn sie noch nicht besucht haben
            //ignoriert gewichtung weil dfs nicht den kürzesten Weg sucht
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

        // wenn kein Pfad gefunden wurde, wird eine Warnung ausgegeben und ein leerer Pfad zurückgegeben.
        if (!found)
        {
            Debug.LogWarning("DFS: Kein Pfad zum Ziel gefunden!");
            return finalPath;
        }

        // Pfad rekonstruieren und Knoten auf dem Pfad merken
        HashSet<Node> pathSet = new HashSet<Node>();
        Node temp = goal;
        while (temp != null)
        {
            finalPath.Push(temp); // Push dreht Ziel->Start in Start->Ziel in vorwärts Reihenfolge um
            pathSet.Add(temp);
            cameFrom.TryGetValue(temp, out temp);
        }

        // Sackgassen / alternative Wege = besuchte Knoten, die nicht auf dem Pfad liegen.
        foreach (Node n in visited)
        {
            if (!pathSet.Contains(n))
                deadEndNodes.Add(n);
        }
        Debug.Log($"DFS: {visitedDFSNodes} Knoten untersucht");
        if (generator.logText != null)
        {
            generator.logText.text += $"DFS: {visitedDFSNodes} Knoten untersucht\n";
        }
        return finalPath;
    }
}