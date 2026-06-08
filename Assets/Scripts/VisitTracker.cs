using UnityEngine;
using System.Collections.Generic;

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
