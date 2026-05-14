using UnityEngine;

public class PixelPerfectCamera : MonoBehaviour
{
    [SerializeField] private float pixelsPerUnit = 16f;

    private void LateUpdate()
    {
        float pixelSize = 1f / pixelsPerUnit;
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / pixelSize) * pixelSize;
        pos.y = Mathf.Round(pos.y / pixelSize) * pixelSize;
        pos.z = -10f; // Keep z constant
        transform.position = pos;
    }
}