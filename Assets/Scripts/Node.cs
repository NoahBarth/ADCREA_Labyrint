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
    public Color color = Color.white; // Optional: Farbe für Visualisierung
}
