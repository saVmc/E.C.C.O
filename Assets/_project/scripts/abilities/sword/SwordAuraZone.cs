using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class SwordAuraZone : MonoBehaviour
{
    private float radius;
    private float slowMult;
    private float duration;

    private LineRenderer lr;
    private float elapsed;
    private float nextTick;

    public void Init(float radius, float slowMult, float duration)
    {
        this.radius   = radius;
        this.slowMult = slowMult;
        this.duration = duration;

        Gun.GlobalDamageMultiplier = 1.1f;

        SetupLineRenderer();
        DrawCircle();
    }

    private void SetupLineRenderer()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop           = true;
        lr.positionCount  = 64;
        lr.useWorldSpace  = false;
        lr.startWidth     = 0.1f;
        lr.endWidth       = 0.1f;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

float lifeT  = 1f - elapsed / duration;
        float pulse  = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.8f);
        float width  = 0.07f + 0.05f * pulse;
        Color colour = new Color(0.75f + pulse * 0.25f, 0.2f, 1f, pulse * lifeT * 0.85f);

        lr.startWidth  = width;
        lr.endWidth    = width;
        lr.startColor  = colour;
        lr.endColor    = colour;

if (Time.time >= nextTick)
        {
            nextTick = Time.time + 1f;
            ApplyZoneEffects();
        }
    }

    private void ApplyZoneEffects()
    {

        if (PlayerHealth.Instance != null)
        {
            float d = Vector2.Distance(transform.position,
                                       PlayerHealth.Instance.transform.position);
            if (d <= radius)
                PlayerHealth.Instance.Heal(1);
        }

foreach (Collider2D c in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead)
                e.ApplySlow(slowMult, 1.6f);
        }
    }

    private void DrawCircle()
    {
        for (int i = 0; i < 64; i++)
        {
            float angle = (float)i / 64 * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius,
                                          Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void OnDestroy()
    {
        Gun.GlobalDamageMultiplier = 1f;
    }
}