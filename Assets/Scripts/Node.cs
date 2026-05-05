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
    public Node parent;

    // Vergleich für die SortedList / SortedSet (Priorisierung nach Kosten)
    public int CompareTo(Node other)
    {
        if (other == null) return 1;
        float thisFCost = gCost + hCost;
        float otherFCost = other.gCost + other.hCost;
        return thisFCost.CompareTo(otherFCost);
    }
}
