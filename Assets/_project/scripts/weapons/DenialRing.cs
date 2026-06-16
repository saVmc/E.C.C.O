using UnityEngine;

/// <summary>
/// Draws a pulsing circle around the player showing the suppressive-fire denial zone.
/// Add to the player GameObject. Gun.cs calls SetRadius() when the upgrade is applied.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class DenialRing : MonoBehaviour
{
    public static DenialRing Instance { get; private set; }

    [SerializeField] private int segments = 48;
    [SerializeField] private Color ringColor = new Color(1f, 0.35f, 0f, 0.6f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseAmplitude = 0.15f;

    private LineRenderer lr;
    private float radius = 0f;
    private bool active = false;

    private void Awake()
    {
        Instance = this;
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = ringColor;
        lr.endColor = ringColor;
        lr.enabled = false;
    }

    private void Update()
    {
        if (!active) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        float r = radius * pulse;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }

        Color c = ringColor;
        c.a = ringColor.a * (0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed));
        lr.startColor = c;
        lr.endColor   = c;
    }

    public void SetRadius(float r)
    {
        radius = r;
        active = r > 0f;
        lr.enabled = active;
    }
}
