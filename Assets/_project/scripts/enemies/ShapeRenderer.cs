using UnityEngine;

/// <summary>
/// Generates a regular polygon sprite at runtime on this GameObject's SpriteRenderer.
/// Add to any enemy prefab. Sides: 3=triangle, 4=square, 5=pentagon, 6=hexagon (boss).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ShapeRenderer : MonoBehaviour
{
    [SerializeField] private int sides = 3;
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private int textureSize = 128;

    private void Awake()
    {
        GetComponent<SpriteRenderer>().sprite = BuildSprite(sides, fillColor, textureSize);
    }

    public static Sprite BuildSprite(int sides, Color color, int size = 64)
    {
        sides = Mathf.Max(3, sides);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = Color.clear;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.45f;

        // Pre-compute polygon vertices
        Vector2[] verts = new Vector2[sides];
        float startAngle = -Mathf.PI * 0.5f; // point upward
        for (int i = 0; i < sides; i++)
        {
            float a = startAngle + i * Mathf.PI * 2f / sides;
            verts[i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        // Fill pixels inside the polygon
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (PointInPolygon(new Vector2(x, y), verts))
                    pixels[y * size + x] = color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size); // pixels per unit = size so world size = 1 unit
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
            j = i;
        }
        return inside;
    }
}
