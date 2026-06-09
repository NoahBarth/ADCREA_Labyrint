using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
            current.color = Color.brown;
            // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
            foreach (Node neighbor in generator.GetWalkableNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                // Betretungskosten der Zelle als Kantengewicht -> Dijkstra weicht von der reinen
                // Schritt-Zählung (BFS) ab und wählt dadurch einen anderen Weg.
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
    /// damit nicht nur der kürzeste Pfad gefunden wird, sondern auch schneller, indem die Suche in Richtung Ziel gelenkt wird.

    // public Stack<Node> FindPathAStar(Node start, Node goal, labyrinthGenerator generator)
    // {
    //     if (start == null || goal == null || generator == null)
    //     {
    //         Debug.LogError("Start, Goal oder Generator ist null!");
    //         return new Stack<Node>();
    //     }

    //     List<Node> allNodes = generator.GetFloorNodes();
    //     HashSet<Node> closedSet = new HashSet<Node>();
    //     List<Node> openList = new List<Node>();

    //     // Initialisierung
    //     foreach (Node node in allNodes)
    //     {
    //         node.gCost = float.MaxValue;
    //         node.hCost = Vector2Int.Distance(node.position, goal.position);
    //         node.parent = null;
    //     }

    //     start.gCost = 0;
    //     openList.Add(start);

    //     while (openList.Count > 0)
    //     {
    //     //hauptschleife: Knoten mit niedrigsten fCost (gCost + hCost) wird ausgewählt & openlist sortiert, damit der Knoten mit den geringsten Kosten immer an erster Stelle steht
    //         openList.Sort(); // Sortiert nach gCost + hCost (F-Kosten)
    //         Node current = openList[0];
    //         openList.RemoveAt(0);

    //         if (current == goal)//wenn ziel erreicht
    //         {
    //             break;
    //         }

    //         closedSet.Add(current);

    //         // Nur echte Gitter-Nachbarn (begehbare Floor-Felder) evaluieren
    //         foreach (Node neighbor in generator.GetWalkableNeighbors(current))
    //         {
    //             if (closedSet.Contains(neighbor))
    //                 continue;

    //             float tentativeGCost = current.gCost + Vector2Int.Distance(current.position, neighbor.position);

    //             if (tentativeGCost < neighbor.gCost)
    //             {
    //                 neighbor.gCost = tentativeGCost;
    //                 neighbor.parent = current;

    //                 if (!openList.Contains(neighbor))
    //                 {
    //                     openList.Add(neighbor);
    //                 }
    //             }
    //         }
    //     }

    //     return BacktrackPath(goal);
    // }

   
    /// DFS (Depth-First Search) zur Pfadfindung. Im Gegensatz zu Dijkstra/A*
    /// findet DFS nicht zwingend den kürzesten Weg, wird aber
    /// alternative Pfade und Sackgassen im Labyrinth aufzeigen.
    /// Gibt den gefundenen Pfad als Stack in Vorwärts-Reihenfolge 
    /// zurück und liefert über deadEndNodes alle besuchten Knoten, die NICHT
    /// auf dem finalen Pfad liegen (= Sackgassen / alternative Wege).
  
    public Stack<Node> FindPathBFS(Node start, Node goal, labyrinthGenerator generator, out List<Node> deadEndNodes)
    {
        deadEndNodes = new List<Node>();
        Stack<Node> finalPath = new Stack<Node>();

        if (start == null || goal == null || generator == null)
        {
            Debug.LogError("BFS: Start, Goal oder Generator ist null!");
            return finalPath;
        }

        HashSet<Node> visited = new HashSet<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        Queue<Node> queue = new Queue<Node>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;

        bool found = false;

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

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
                    neighbor.color = Color.yellow; // Optional: Markiere besuchte Knoten visuell
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("BFS: Kein Pfad zum Ziel gefunden!");
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

    /// Findet alle Sackgassen im Labyrinth. Ein Feld ist ein Sackgassen-Ende, wenn es nur EINEN begehbaren Nachbarn hat (Grad 1).
    /// Von jedem solchen Ende wird der Korridor bis zur nächsten Kreuzung
    /// (Grad >= 3) verfolgt; alle Korridorfelder bis dorthin gelten als Sackgasse.
    /// Start- und Zielfeld werden ausgenommen.

    public List<Node> FindDeadEnds(labyrinthGenerator generator)
    {
        List<Node> deadEndCells = new List<Node>();
        if (generator == null)
        {
            Debug.LogError("FindDeadEnds: Generator ist null!");
            return deadEndCells;
        }

        HashSet<Node> marked = new HashSet<Node>();

        foreach (Node node in generator.GetFloorNodes())
        {
            if (node == generator.StartNode || node == generator.GoalNode)
                continue;

            // Nur echte Sackgassen-Enden starten den Korridor-Lauf.
            if (generator.GetWalkableNeighbors(node).Count != 1)
                continue;

            Node current = node;
            Node previous = null;

            while (current != null
                   && current != generator.StartNode
                   && current != generator.GoalNode)
            {
                List<Node> neighbors = generator.GetWalkableNeighbors(current);

                // Kreuzung erreicht -> Korridor endet (Kreuzung selbst nicht markieren).
                if (neighbors.Count >= 3)
                    break;

                // Schon markiert (z. B. von einer anderen Sackgasse) -> abbrechen.
                if (!marked.Add(current))
                    break;

                deadEndCells.Add(current);

                // Weiter zum einzigen Nachbarn, der nicht woher wir kamen ist.
                Node next = null;
                foreach (Node n in neighbors)
                {
                    if (n != previous)
                    {
                        next = n;
                        break;
                    }
                }

                previous = current;
                current = next;
            }
        }

        return deadEndCells;
    }
}