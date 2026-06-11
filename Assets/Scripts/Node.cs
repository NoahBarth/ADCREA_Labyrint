using UnityEngine;
using System;

public enum TileType { Wall, Floor }

public class Node : IComparable<Node>
{
    public Vector2Int position;
    public TileType type;
    public GameObject visual;

    // Für Dijkstra & A* Algorithmus
    public float gCost = float.MaxValue;
    public float hCost = 0f; // Heuristik für A*
    public float weight = 1f; // Betretungskosten dieser Zelle (Dijkstra wertet sie aus, BFS ignoriert sie)
    public Node parent;

    // F-Kosten = gCost + hCost. Sortiert die Open-List (openList.Sort()) so,
    // dass der günstigste Knoten vorne steht. Bei A* zählt die Heuristik mit,
    // bei Dijkstra ist hCost = 0.
    // Als normale Property (statt Lambda/Arrow-Schreibweise) geschrieben.
    public float fCost
    {
        get { return gCost + hCost; }
    }

    // Vergleicht zwei Knoten anhand ihrer F-Kosten. Dadurch kann die Open-List
    // mit openList.Sort() OHNE Lambda sortiert werden: Der Standard-Comparer von
    // List<Node> ruft automatisch diese CompareTo-Methode auf.
    // Bei Dijkstra ist hCost = 0, daher entspricht fCost dort exakt gCost.
    public int CompareTo(Node other)
    {
        if (other == null)
        {
            return 1;
        }
        return fCost.CompareTo(other.fCost);
    }

}
