
/// Binary Search Tree für Zeitstempel-basierte Besuchsreihenfolge
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