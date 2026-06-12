using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Pathfinder))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private labyrinthGenerator generator;
    public float moveSpeed = 300f;
    public enum PathfindingAlgorithm { Dijkstra, AStar, DFS }
    public PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar;

    private Pathfinder pathfinder;
    private int trailLane;
    private Color trailColor = Color.white;
    private HashSet<Vector2Int> markedCells = new HashSet<Vector2Int>();
    private Node lastTrailNode;

    // Wenn true, wartet der Spieler mit dem Loslaufen, bis Release() aufgerufen wird.
    // Der Generator nutzt das, um den Spieler erst nach seiner Pfad-Einfärbung zu starten.
    public bool waitForRelease = false;
    private bool released = false;

    private Stack<Node> assignedPath;

    public void Release()
    {
        released = true;
    }

    public void SetGenerator(labyrinthGenerator gen)
    {
        generator = gen;
    }

    public void SetTrailLane(int lane)
    {
        trailLane = lane;
    }

    public void SetTrailColor(Color color)
    {
        trailColor = color;
    }

    public void SetPath(Stack<Node> path)
    {
        assignedPath = new Stack<Node>(path);
    }


    private void Awake()
    {
        pathfinder = GetComponent<Pathfinder>();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        sr.color = Color.red;
        transform.localScale = new Vector3(0.8f, 0.8f, 1f);
    }

    private IEnumerator Start()
    {
        // Wait one frame so labyrinthGenerator.Start() has finished
        yield return null;

        if (generator == null)
            generator = FindAnyObjectByType<labyrinthGenerator>();

        if (generator == null || !generator.IsReady)
        {
            Debug.LogError("PlayerController: labyrinthGenerator nicht gefunden oder nicht bereit.");
            yield break;
        }

        List<Node> deadEnds = new List<Node>();

        Stack<Node> path = assignedPath;

        if (path == null || path.Count == 0)
        {
            Debug.LogError("PlayerController: Kein Pfad gefunden!");
            yield break;
        }
        transform.position = generator.GetWorldPosition(generator.StartNode);
        lastTrailNode = generator.StartNode;

        // Warten, bis der Generator diesen Spieler freigibt (nach seiner Pfad-Einfärbung).
        while (waitForRelease && !released)
            yield return null;

        yield return StartCoroutine(MoveAlongPath(path));
        Debug.Log("Ziel erreicht!");
    }

    private IEnumerator MoveAlongPath(Stack<Node> path)
    {
        while (path.Count > 0)
        {
            Node nextNode = path.Pop();
            Vector3 target = generator.GetWorldPosition(nextNode);

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            lastTrailNode = nextNode;
        }
    }

}
