using UnityEngine;
using UnityEngine.Tilemaps;

public class PixelPerfectCamera : MonoBehaviour
{
    [SerializeField] private float pixelsPerUnit = 16f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Tilemap tilemap;
    [Tooltip("Smoothing time in seconds for SmoothDamp. Smaller = snappier, larger = smoother.")]
    [SerializeField] private float smoothTime = 0.08f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float camHalfWidth;
    private float camHalfHeight;
    private Vector3 minCamPos;
    private Vector3 maxCamPos;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (tilemap == null)
            tilemap = FindAnyObjectByType<Tilemap>();

        CalculateCameraExtents();
    }

    private void Start()
    {
        // Recalculate after all Awakes have run — tilemap may not be ready in Awake
        CalculateCameraExtents();
    }

    public void RecalculateBounds() => CalculateCameraExtents();

    private void CalculateCameraExtents()
    {
        if (cam == null) cam = Camera.main;
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;

        if (tilemap != null)
        {
            BoundsInt b = tilemap.cellBounds;
            Vector3 worldMin = tilemap.CellToWorld(b.min);
            Vector3 worldMax = tilemap.CellToWorld(b.max);

            if (worldMax.x - worldMin.x <= camHalfWidth * 2f)
            {
                float cx = (worldMin.x + worldMax.x) * 0.5f;
                minCamPos.x = maxCamPos.x = cx;
            }
            else
            {
                minCamPos.x = worldMin.x + camHalfWidth;
                maxCamPos.x = worldMax.x - camHalfWidth;
            }

            if (worldMax.y - worldMin.y <= camHalfHeight * 2f)
            {
                float cy = (worldMin.y + worldMax.y) * 0.5f;
                minCamPos.y = maxCamPos.y = cy;
            }
            else
            {
                minCamPos.y = worldMin.y + camHalfHeight;
                maxCamPos.y = worldMax.y - camHalfHeight;
            }

            minCamPos.z = maxCamPos.z = -10f;
        }
        else
        {
            minCamPos = new Vector3(-10000f, -10000f, -10f);
            maxCamPos = new Vector3(10000f, 10000f, -10f);
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 target = playerTransform.position;
        target.z = -10f;

        target.x = Mathf.Clamp(target.x, minCamPos.x, maxCamPos.x);
        target.y = Mathf.Clamp(target.y, minCamPos.y, maxCamPos.y);

        float pixelSize = 1f / pixelsPerUnit;
        target.x = Mathf.Round(target.x / pixelSize) * pixelSize;
        target.y = Mathf.Round(target.y / pixelSize) * pixelSize;

        Vector3 newPos = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        newPos.z = -10f;

        newPos.x = Mathf.Clamp(newPos.x, minCamPos.x, maxCamPos.x);
        newPos.y = Mathf.Clamp(newPos.y, minCamPos.y, maxCamPos.y);

        transform.position = newPos;
    }
}