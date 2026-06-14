using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class EMPRingEffect : MonoBehaviour
{
    private LineRenderer lr;
    private float maxRadius;
    private float duration;
    private float elapsed;
    private int segments = 48;

    public static void Spawn(Vector3 position, float radius, float duration = 0.45f)
    {
        GameObject obj = new GameObject("EMPRing");
        obj.transform.position = position;
        EMPRingEffect effect = obj.AddComponent<EMPRingEffect>();
        effect.Init(radius, duration);
    }

    private void Init(float radius, float dur)
    {
        maxRadius = radius;
        duration = dur;
        elapsed = 0f;

        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = segments;
        lr.useWorldSpace = false;
        lr.startWidth = 0.12f;
        lr.endWidth = 0.12f;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.3f, 0.8f, 1f, 1f);
        lr.endColor = new Color(0.3f, 0.8f, 1f, 1f);

        DrawCircle(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float currentRadius = Mathf.Lerp(0f, maxRadius, t);
        float alpha = Mathf.Lerp(1f, 0f, t);

        DrawCircle(currentRadius);
        lr.startColor = new Color(0.3f, 0.8f, 1f, alpha);
        lr.endColor = new Color(0.3f, 0.8f, 1f, alpha);
    }

    private void DrawCircle(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}
