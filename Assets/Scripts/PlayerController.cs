using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Pathfinder))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private labyrinthGenerator generator;
    public float moveSpeed = 20f;
    public enum PathfindingAlgorithm { Dijkstra, AStar, BFS }
    public PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar;

    private Pathfinder pathfinder;

    public void SetGenerator(labyrinthGenerator gen) {
        generator = gen;
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

        Stack<Node> path;
        List<Node> deadEnds = new List<Node>();

        switch (algorithm)
        {
            case PathfindingAlgorithm.Dijkstra:
                path = pathfinder.FindPathDijkstra(generator.StartNode, generator.GoalNode, generator);
                break;
            case PathfindingAlgorithm.BFS:
                path = pathfinder.FindPathDFS(generator.StartNode, generator.GoalNode, generator, out deadEnds);
                break;
            default:
                path = pathfinder.FindPathDijkstra(generator.StartNode, generator.GoalNode, generator);
                break;
        }

        if (path == null || path.Count == 0)
        {
            Debug.LogError("PlayerController: Kein Pfad gefunden!");
            yield break;
        }

        Debug.Log($"Pfad gefunden: {path.Count} Schritte");
        transform.position = generator.GetWorldPosition(generator.StartNode);

        yield return StartCoroutine(MoveAlongPath(path));
        Debug.Log("Ziel erreicht!");
    }

    private IEnumerator MoveAlongPath(Stack<Node> path)
    {
        while (path.Count > 0)
        {
            Vector3 target = generator.GetWorldPosition(path.Pop());

            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
        }
    }
}
