using UnityEngine;

/// <summary>
/// Attach to any UI Image that should fill the entire canvas.
/// Fixes the "black bars around overlay in build" issue by forcing full stretch at runtime.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class FullscreenStretch : MonoBehaviour
{
    private void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
