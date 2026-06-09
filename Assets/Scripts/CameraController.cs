using UnityEngine;
using System.Collections;
using TMPro;


public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform labyrinthRoot;
    [SerializeField] private bool autoFitOnStart = true;
    [SerializeField] private Vector2 padding = new Vector2(1f, 1f);

    private void Start()
    {
        if (autoFitOnStart)
        {
            StartCoroutine(FitAfterGeneration());
        }
    }

    public void FitToLabyrinth()
    {
        if (labyrinthRoot == null)
        {
            labyrinthRoot = FindAnyObjectByType<labyrinthGenerator>()?.transform;
        }

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

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        if (!Camera.main || !Camera.main.orthographic)
        {
            Debug.LogWarning("CameraController: Main camera is missing or not orthographic.");
            return;
        }

        Camera camera = Camera.main;
        Vector3 center = bounds.center;
        float targetHeight = (bounds.size.y * 0.5f) + padding.y;
        float targetWidth = ((bounds.size.x * 0.5f) + padding.x) / camera.aspect;

        camera.orthographicSize = Mathf.Max(targetHeight, targetWidth);
        camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
    }

    private IEnumerator FitAfterGeneration()
    {
        yield return null;
        FitToLabyrinth();
    }

}
