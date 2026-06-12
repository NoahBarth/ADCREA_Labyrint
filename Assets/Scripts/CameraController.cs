using UnityEngine;
using System.Collections;
using TMPro;

// dieser Skript steuert die Kamera, um das Labyrinth optimal anzuzeigen. 
// Es berechnet die Grenzen des Labyrinths und passt die Kamera entsprechend an, 
// damit das gesamte Labyrinth sichtbar ist. Es bietet auch eine Option, die Kamera automatisch nach der Generierung des Labyrinths anzupassen.
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform labyrinthRoot;
    [SerializeField] private Vector2 padding = new Vector2(1f, 1f);
    private void Start()
    {
        StartCoroutine(FitAfterGeneration());
    }

    // Wartet einen Frame, damit der Generator Zeit hat, das Labyrinth zu erstellen, bevor die Kamera angepasst wird.
    private IEnumerator FitAfterGeneration()
    {
        yield return null;
        FitToLabyrinth();
    }

    // Berechnet die Grenzen des Labyrinths und passt die Kamera entsprechend an.
    public void FitToLabyrinth()
    {
        if (labyrinthRoot == null)
        {
            labyrinthGenerator generator = FindAnyObjectByType<labyrinthGenerator>();
            if (generator != null)
            {
                labyrinthRoot = generator.transform;
            }
        }

        // Wenn kein Labyrinth-Root gefunden wird, wird eine Warnung ausgegeben und die Anpassung übersprungen.
        if (labyrinthRoot == null)
        {
            Debug.LogWarning("CameraController: No labyrinth root found, skipping camera fit.");
            return;
        }

        Renderer[] renderers = labyrinthRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("CameraController: No renderers found under labyrinth root.");
            return;
        }

        // Berechnet die Gesamtgrenzen aller Renderer, um die Größe des Labyrinths zu bestimmen.
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Falls die Kamera fehlt oder nicht orthografisch ist, wird eine Warnung ausgegeben und die Anpassung übersprungen.
        if (!Camera.main || !Camera.main.orthographic)
        {
            Debug.LogWarning("CameraController: Main camera is missing or not orthographic.");
            return;
        }

        // Berechnet die erforderliche orthografische Größe, um das gesamte Labyrinth mit Padding anzuzeigen, und positioniert die Kamera entsprechend.
        Camera camera = Camera.main;
        Vector3 center = bounds.center;
        float targetHeight = (bounds.size.y * 0.5f) + padding.y;
        float targetWidth = ((bounds.size.x * 0.5f) + padding.x) / camera.aspect;

        // Setzt die orthografische Größe auf den größeren Wert von Höhe oder Breite, um sicherzustellen, dass das gesamte Labyrinth sichtbar ist.
        camera.orthographicSize = Mathf.Max(targetHeight, targetWidth);
        camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
    }
}
