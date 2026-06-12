using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private labyrinthGenerator generator;
    public float moveSpeed = 300f;

    // Auswahl des Pfadfindungsalgorithmus, der für diesen Spieler verwendet werden soll. Dijkstra und A* berücksichtigen die Gewichtung der Knoten, DFS ignoriert sie.
    public enum PathfindingAlgorithm { Dijkstra, AStar, DFS }


    public PathfindingAlgorithm algorithm = PathfindingAlgorithm.AStar;

    // Wenn true, wartet der Spieler mit dem Loslaufen, bis Release() aufgerufen wird.
    // Der Generator nutzt das, um den Spieler erst nach seiner Pfad-Einfärbung zu starten.
    public bool waitForRelease = false; //startsignal, wenn path fertig generiert, kann player loslaufen
    private bool released = false;

    private Stack<Node> assignedPath;

    public void Release()
    {
        released = true; // Startsignal für den Spieler, um loszulaufen, nachdem der Generator den Pfad eingefärbt hat.
    }

    public void SetGenerator(labyrinthGenerator gen)
    {
        generator = gen;
    }

    public void SetPath(Stack<Node> path)
    {
        assignedPath = new Stack<Node>(path);
    }

// vor start wird ein Objekt erzeugt, damit der Spieler eine Sichtbare darsteelung hat 
    private void Awake()
    {        
        //falls kein sprite gesetzt  ist, wird ein weisser sprite erstellt. 
       
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        //standardfarbe auf rot, wird aber über inspector überschrieben 
        sr.color = Color.red;
        transform.localScale = new Vector3(0.8f, 0.8f, 1f);
    }

// Labyrinthgenerator muss zuerst fertig sein, damit der player den pfad bekommt. 
    private IEnumerator Start()
    {

        yield return null; //wartet einen frame, damit andere Start()-Methode
        
        // kein Generator gesetzt = sucht in der Szene 
        if (generator == null)
            generator = FindAnyObjectByType<labyrinthGenerator>();

        // immer noch nicht fertig, prozess abbrechen
        if (generator == null || !generator.IsReady)
        {
            Debug.LogError("PlayerController: labyrinthGenerator nicht gefunden oder nicht bereit.");
            yield break;
        }

        Stack<Node> path = assignedPath;

        if (path == null || path.Count == 0)
        {
            Debug.LogError("PlayerController: Kein Pfad gefunden!");
            yield break;
        }
        transform.position = generator.GetWorldPosition(generator.StartNode);

        // Warten, bis der Generator diesen Spieler freigibt (nach seiner Pfad-Einfärbung).
        while (waitForRelease && !released)
            yield return null;

        yield return StartCoroutine(MoveAlongPath(path));
        Debug.Log("Ziel erreicht!");
    }


//Laufanimation, die den Spieler entlang des zugewiesenen Pfads bewegt.
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
        }
    }
}
