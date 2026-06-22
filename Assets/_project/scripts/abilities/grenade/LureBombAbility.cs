using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public sealed class LureBombAbility : Ability
{
    private float throwRange      = 12f;
    private float lureRadius      = 5f;
    private float lureDuration    = 1.5f;
    private float blastRadius     = 4.5f;
    private int   blastDamage     = 28;
    private bool  leavesBurnZone  = false;
    private float burnDuration    = 2.5f;
    private int   burnDamage      = 4;
    private bool  fragments       = false;
    private int   fragmentCount   = 3;
    private bool  novaProtocol    = false;
    private float lureVulnMult    = 1.75f;

    private Rigidbody2D rb;

    // Saved during cinematic so OnDisable can restore them if the coroutine is cut short
    private Camera   cinematicCam;
    private Vector3  cinematicCamOrigPos;
    private float    cinematicCamOrigSize;
    private PixelPerfectCamera cinematicPixelCam;
    private Canvas[] cinematicCanvases;
    private bool[]   cinematicCanvasStates;
    private bool     isCinematicActive = false;

    private void Awake() => rb = GetComponentInParent<Rigidbody2D>();

    private void OnDisable()
    {
        if (!isCinematicActive) return;
        isCinematicActive = false;
        if (cinematicCam != null)
        {
            cinematicCam.transform.position = cinematicCamOrigPos;
            cinematicCam.orthographicSize   = cinematicCamOrigSize;
        }
        if (cinematicPixelCam != null) cinematicPixelCam.enabled = true;
        if (cinematicCanvases != null)
            for (int i = 0; i < cinematicCanvases.Length; i++)
                if (cinematicCanvases[i] != null) cinematicCanvases[i].enabled = cinematicCanvasStates[i];
        if (Time.timeScale < 0.5f) Time.timeScale = 1f;
    }

    protected override void Activate()
    {
        Vector2 origin    = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 screen    = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector2 mousePos  = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
        Vector2 dir       = (mousePos - origin);
        float   rawDist   = dir.magnitude;
        Vector2 target    = origin + dir.normalized * Mathf.Min(rawDist, throwRange);
        StartCoroutine(BombRoutine(origin, target));
    }

    private IEnumerator BombRoutine(Vector2 origin, Vector2 target)
    {
        GameObject bombGO = CreateBombVisual(new Color(1f, 0.75f, 0f));
        float throwDur = Mathf.Clamp(Vector2.Distance(origin, target) / 22f, 0.1f, 0.35f);
        float t = 0f;
        while (t < throwDur)
        {
            t += Time.deltaTime;
            float ease = Mathf.SmoothStep(0f, 1f, t / throwDur);
            if (bombGO != null)
            {
                bombGO.transform.position = Vector3.Lerp(origin, target, ease);
                bombGO.transform.rotation = Quaternion.identity; // feet always down
            }
            yield return null;
        }
        if (bombGO != null) bombGO.transform.position = target;

        if (novaProtocol)
        {
            yield return StartCoroutine(CinematicNovaDuck(bombGO, target));
            yield break;
        }

        EMPRingEffect.Spawn(target, lureRadius * 0.4f, 0.3f);

        float lureElapsed = 0f;
        while (lureElapsed < lureDuration)
        {
            lureElapsed += Time.deltaTime;
            if (bombGO != null)
            {
                float pulse = 0.9f + Mathf.Sin(lureElapsed * 10f) * 0.15f;
                bombGO.transform.localScale = Vector3.one * pulse * 3.0f;
                bombGO.GetComponent<SpriteRenderer>().color =
                    Color.Lerp(new Color(1f, 0.75f, 0f), new Color(1f, 0.2f, 0f),
                               Mathf.Clamp01(lureElapsed / lureDuration));
            }

            foreach (Collider2D c in Physics2D.OverlapCircleAll(target, lureRadius))
            {
                Enemy e = c.GetComponentInParent<Enemy>();
                if (e != null && !e.IsDead) e.PullToward(target, 7f);
            }
            yield return null;
        }

        if (bombGO != null) Destroy(bombGO);
        Explode(target);
    }

    private void Explode(Vector2 center)
    {
        EMPRingEffect.Spawn(center, blastRadius * 0.35f, 0.2f);
        EMPRingEffect.Spawn(center, blastRadius * 0.7f,  0.4f);
        EMPRingEffect.Spawn(center, blastRadius,         0.6f);

        if (novaProtocol)
            EMPRingEffect.Spawn(center, blastRadius * 1.6f, 0.9f);

        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, blastRadius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;

            float vuln = novaProtocol ? lureVulnMult : 1f;
            e.TakeDamage(Mathf.RoundToInt(blastDamage * vuln));

            if (novaProtocol) e.ApplySlow(0f, 1.2f);

            Vector2 kd = ((Vector2)e.transform.position - center).normalized;
            if (kd.sqrMagnitude < 0.01f) kd = Random.insideUnitCircle.normalized;
            e.ForceKnockback(kd, 14f);
        }

        if (leavesBurnZone) StartCoroutine(BurnZone(center, blastRadius * 0.75f, burnDuration));
        if (fragments)      StartCoroutine(FragmentSalvo(center));
    }

    private IEnumerator BurnZone(Vector2 center, float radius, float duration)
    {
        LineRenderer ring = MakeRing(center, radius, new Color(1f, 0.35f, 0f, 0.7f));
        float elapsed = 0f, nextTick = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextTick)
            {
                nextTick += 0.5f;
                foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && !e.IsDead) e.TakeDamage(burnDamage);
                }
                EMPRingEffect.Spawn(center, radius * 0.6f, 0.25f);
            }
            if (ring != null)
                ring.startColor = ring.endColor = new Color(1f, 0.35f, 0f, Mathf.Lerp(0.7f, 0f, elapsed / duration));
            yield return null;
        }
        if (ring != null) Destroy(ring.gameObject);
    }

    private IEnumerator FragmentSalvo(Vector2 center)
    {
        yield return new WaitForSeconds(0.05f);
        for (int i = 0; i < fragmentCount; i++)
        {
            float   angle  = (i / (float)fragmentCount) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector2 fTarget = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(2f, 3.5f);
            StartCoroutine(FragDelay(fTarget));
        }
    }

    private IEnumerator FragDelay(Vector2 pos)
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.35f));
        EMPRingEffect.Spawn(pos, blastRadius * 0.45f, 0.3f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, blastRadius * 0.5f))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead) e.TakeDamage(Mathf.CeilToInt(blastDamage * 0.45f));
        }
    }

    private GameObject CreateBombVisual(Color fallbackColor)
    {
        GameObject go = new GameObject("_LureBomb");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50;

        Sprite customSprite = definition != null ? definition.VisualSprite : null;
        if (customSprite != null)
        {
            sr.sprite = customSprite;
            sr.color  = Color.white;
        }
        else
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), Vector2.one * 7.5f) < 7f
                        ? Color.white : Color.clear);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16f);
            sr.color  = fallbackColor;
        }

        go.transform.localScale = Vector3.one * 3.0f;
        return go;
    }

    private LineRenderer MakeRing(Vector2 center, float radius, Color color)
    {
        GameObject   go  = new GameObject("_Ring");
        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = 32;
        lr.startWidth = lr.endWidth = 0.07f;
        lr.startColor = lr.endColor = color;
        lr.sortingOrder = 30;
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (sh != null) lr.material = new Material(sh);
        for (int i = 0; i < 32; i++)
        {
            float a = (i / 32f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius,
                                          center.y + Mathf.Sin(a) * radius, 0f));
        }
        return lr;
    }

    // ─── ★5 Cinematic — The Duck Goes Boom ───────────────────────────────────

    private IEnumerator CinematicNovaDuck(GameObject duck, Vector2 center)
    {
        Camera cam = Camera.main;
        PixelPerfectCamera pixelCam = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;
        if (pixelCam != null) pixelCam.enabled = false;

        Canvas[] canvases     = FindObjectsByType<Canvas>();
        bool[]   canvasStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
            { canvasStates[i] = canvases[i].enabled; canvases[i].enabled = false; }

        Vector3 camOrigPos  = cam != null ? cam.transform.position : Vector3.back * 10f;
        float   camOrigSize = cam != null ? cam.orthographicSize : 5f;

        // Save for OnDisable safety net
        cinematicCam            = cam;
        cinematicCamOrigPos     = camOrigPos;
        cinematicCamOrigSize    = camOrigSize;
        cinematicPixelCam       = pixelCam;
        cinematicCanvases       = canvases;
        cinematicCanvasStates   = canvasStates;
        isCinematicActive       = true;
        Vector3 duckPos3    = new Vector3(center.x, center.y, camOrigPos.z);
        SpriteRenderer duckSR = duck != null ? duck.GetComponent<SpriteRenderer>() : null;

        // ── ACT I: SO CUTE (1.5s) ────────────────────────────────────────────
        // Camera drifts slowly toward the duck. Sparkles orbit it.
        // Enemies are still being lured in. Everything is fine. :)
        StartCoroutine(LureEnemiesDuring(center, 2.3f));
        StartCoroutine(DuckSparkles(center, 1.5f));

        float e = 0f;
        while (e < 1.5f)
        {
            e += Time.unscaledDeltaTime;
            float slowT = Mathf.SmoothStep(0f, 1f, e / 1.5f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camOrigPos, duckPos3 + (camOrigPos - duckPos3) * 0.30f, slowT * 0.6f);
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(camOrigSize, camOrigSize * 0.60f, slowT * 0.45f);
            }
            if (duck != null)
            {
                float bob = Mathf.Sin(e * 3.5f) * 0.10f;
                duck.transform.position   = new Vector3(center.x, center.y + bob, 0f);
                duck.transform.localScale = Vector3.one * 3.0f;
                if (duckSR != null) duckSR.color = Color.white;
            }
            yield return null;
        }

        // ── ACT II: uh oh. (0.7s) ────────────────────────────────────────────
        // Duck starts shaking. Camera zooms in fast. Everything goes red.
        Vector3 camMidPos  = cam != null ? cam.transform.position : duckPos3;
        float   camMidSize = cam != null ? cam.orthographicSize : camOrigSize;

        e = 0f;
        while (e < 0.70f)
        {
            e += Time.unscaledDeltaTime;
            float t = e / 0.70f;
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camMidPos, duckPos3, Mathf.SmoothStep(0f, 1f, t));
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(camMidSize, camOrigSize * 0.22f, Mathf.SmoothStep(0f, 1f, t));
            }
            if (duck != null && duckSR != null)
            {
                float shake = Mathf.Lerp(0.04f, 0.26f, t);
                duck.transform.position = new Vector3(
                    center.x + UnityEngine.Random.Range(-shake, shake),
                    center.y + UnityEngine.Random.Range(-shake, shake), 0f);
                float flashSpd = Mathf.Lerp(7f, 30f, t);
                float flash    = Mathf.Abs(Mathf.Sin(e * flashSpd));
                duckSR.color = Color.Lerp(Color.white, new Color(1f, 0.12f, 0.04f), flash * t);
                duck.transform.localScale = Vector3.one * Mathf.Lerp(3.0f, 4.2f, t);
            }
            yield return null;
        }

        // ── ACT III: BAM ─────────────────────────────────────────────────────
        // Hard freeze. The universe goes white. Then BOOM.
        Time.timeScale = 0f;
        StartCoroutine(DuckImpactFlash(center, 0.12f, 0.16f));
        yield return new WaitForSecondsRealtime(0.28f);
        Time.timeScale = 1f;

        if (duck != null) Destroy(duck);

        float bigBlast = blastRadius * 2.4f;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, bigBlast))
        {
            Enemy en = c.GetComponentInParent<Enemy>();
            if (en == null || en.IsDead) continue;
            en.TakeDamage(Mathf.RoundToInt(blastDamage * lureVulnMult));
            Vector2 kd = ((Vector2)en.transform.position - center).normalized;
            if (kd.sqrMagnitude < 0.01f) kd = UnityEngine.Random.insideUnitCircle.normalized;
            en.ForceKnockback(kd, 22f);
            en.ApplySlow(0f, 1.5f);
        }

        StartCoroutine(DuckBloom(center, bigBlast * 5.5f, new Color(1f,    1f,    1f,    1.00f), 0.22f));
        StartCoroutine(DuckBloom(center, bigBlast * 3.2f, new Color(1f,    0.70f, 0.15f, 0.90f), 0.48f));
        StartCoroutine(DuckBloom(center, bigBlast * 2.0f, new Color(1f,    0.30f, 0.05f, 0.72f), 0.70f));
        StartCoroutine(DuckStarburst(center, 36, bigBlast * 1.3f, 0.16f));

        EMPRingEffect.Spawn(center, bigBlast * 0.30f, 0.13f);
        EMPRingEffect.Spawn(center, bigBlast * 0.60f, 0.25f);
        EMPRingEffect.Spawn(center, bigBlast,         0.42f);
        EMPRingEffect.Spawn(center, bigBlast * 1.45f, 0.62f);
        EMPRingEffect.Spawn(center, bigBlast * 2.00f, 0.88f);

        if (cam != null) StartCoroutine(ShakeDuckCam(cam, 0.55f, 1.1f));
        if (leavesBurnZone) StartCoroutine(BurnZone(center, bigBlast * 0.55f, burnDuration));

        yield return new WaitForSecondsRealtime(0.55f);

        // The quack. RIP little duck.
        EMPRingEffect.Spawn(center, 1.8f, 0.6f);

        yield return new WaitForSecondsRealtime(0.35f);

        // ── ACT IV: RETURN ───────────────────────────────────────────────────
        Vector3 camPostPos  = cam != null ? cam.transform.position : camOrigPos;
        float   camPostSize = cam != null ? cam.orthographicSize : camOrigSize;

        e = 0f;
        while (e < 0.55f)
        {
            e += Time.unscaledDeltaTime; // unscaled so level-up timeScale=0 can't freeze this
            float rt = Mathf.SmoothStep(0f, 1f, e / 0.55f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camPostPos, camOrigPos, rt);
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(camPostSize, camOrigSize, rt);
            }
            yield return null;
        }
        if (cam != null) { cam.transform.position = camOrigPos; cam.orthographicSize = camOrigSize; }

        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
        if (pixelCam != null) pixelCam.enabled = true;

        isCinematicActive = false;
    }

    // ─── Cinematic helpers ────────────────────────────────────────────────────

    private IEnumerator LureEnemiesDuring(Vector2 center, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (Collider2D c in Physics2D.OverlapCircleAll(center, lureRadius))
            {
                Enemy en = c.GetComponentInParent<Enemy>();
                if (en != null && !en.IsDead) en.PullToward(center, 7f);
            }
            yield return null;
        }
    }

    private IEnumerator DuckSparkles(Vector2 center, float duration)
    {
        const int count = 8;
        var lrs    = new LineRenderer[count];
        var angles = new float[count];
        var cols   = new Color[count];

        for (int i = 0; i < count; i++)
        {
            angles[i] = (i / (float)count) * Mathf.PI * 2f;
            lrs[i]    = MakeDuckLR(0.09f, 51);
            float hue = UnityEngine.Random.Range(0.08f, 0.18f);
            cols[i]   = Color.HSVToRGB(hue, 0.55f, 1f);
            lrs[i].startColor = lrs[i].endColor = cols[i];
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = elapsed < duration * 0.65f ? 1f
                : 1f - (elapsed - duration * 0.65f) / (duration * 0.35f);

            for (int i = 0; i < count; i++)
            {
                if (lrs[i] == null) continue;
                angles[i] += (i % 2 == 0 ? 1f : -1.2f) * Time.unscaledDeltaTime * 2.2f;
                float r       = 0.9f + Mathf.Sin(elapsed * 3f + i) * 0.18f;
                float twinkle = 0.5f + Mathf.Abs(Mathf.Sin(elapsed * 9f + i * 1.5f)) * 0.5f;
                Vector3 pos   = new Vector3(center.x + Mathf.Cos(angles[i]) * r,
                                            center.y + Mathf.Sin(angles[i]) * r * 0.65f, 0f);
                lrs[i].SetPosition(0, pos);
                lrs[i].SetPosition(1, pos + Vector3.up * 0.07f);
                lrs[i].startColor = lrs[i].endColor =
                    new Color(cols[i].r, cols[i].g, cols[i].b, twinkle * fade);
            }
            yield return null;
        }
        foreach (var lr in lrs) if (lr != null) Destroy(lr.gameObject);
    }

    private IEnumerator DuckBloom(Vector2 center, float radius, Color col, float dur)
    {
        EnsureDuckCircle();
        var go  = new GameObject("_DuckBloom");
        var sr  = go.AddComponent<SpriteRenderer>();
        sr.sprite = duckCircleSprite; sr.sortingOrder = 945;
        var sh  = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        var mat = sh != null ? new Material(sh) : null;
        if (mat != null) sr.material = mat;
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * radius * 2f;
        sr.color = col;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            if (sr != null) sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / dur));
            yield return null;
        }
        if (mat != null) Destroy(mat);
        Destroy(go);
    }

    private IEnumerator DuckImpactFlash(Vector2 center, float hold, float fade)
    {
        var go  = new GameObject("_DuckFlash");
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sr.sortingOrder = 901;
        sr.color        = Color.white;
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * 1000f;

        yield return new WaitForSecondsRealtime(hold);
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / fade));
            yield return null;
        }
        Destroy(tex);
        if (go != null) Destroy(go);
    }

    private IEnumerator ShakeDuckCam(Camera cam, float magnitude, float dur)
    {
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float rm = magnitude * (1f - elapsed / dur);
            rm *= rm;
            cam.transform.position = basePos + new Vector3(
                UnityEngine.Random.Range(-rm, rm),
                UnityEngine.Random.Range(-rm, rm), 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    private IEnumerator DuckStarburst(Vector2 center, int count, float maxLen, float growDur)
    {
        var lrs  = new LineRenderer[count];
        var dirs = new Vector2[count];
        var lens = new float[count];

        for (int i = 0; i < count; i++)
        {
            float a  = (i / (float)count) * Mathf.PI * 2f;
            dirs[i]  = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            lens[i]  = maxLen * UnityEngine.Random.Range(0.45f, 1f);
            lrs[i]   = MakeDuckLR(UnityEngine.Random.Range(0.03f, 0.07f), 44);
            lrs[i].startColor = new Color(1f, 0.88f, 0.30f, 1f);
            lrs[i].endColor   = new Color(1f, 0.45f, 0.10f, 0f);
            lrs[i].endWidth   = 0f;
            Vector3 from = new Vector3(center.x, center.y, 0f);
            lrs[i].SetPosition(0, from); lrs[i].SetPosition(1, from);
        }

        float t = 0f;
        while (t < growDur)
        {
            t += Time.unscaledDeltaTime;
            float frac = Mathf.SmoothStep(0f, 1f, t / growDur);
            for (int i = 0; i < count; i++)
            {
                if (lrs[i] == null) continue;
                lrs[i].SetPosition(1, new Vector3(center.x + dirs[i].x * lens[i] * frac,
                                                   center.y + dirs[i].y * lens[i] * frac, 0f));
            }
            yield return null;
        }

        float fadeDur = 0.45f, ft = 0f;
        while (ft < fadeDur)
        {
            ft += Time.unscaledDeltaTime; float frac = ft / fadeDur;
            for (int i = 0; i < count; i++)
            {
                if (lrs[i] == null) continue;
                Color sc = lrs[i].startColor;
                lrs[i].startColor = new Color(sc.r, sc.g, sc.b, Mathf.Lerp(sc.a, 0f, frac));
            }
            yield return null;
        }
        foreach (var lr in lrs) if (lr != null) Destroy(lr.gameObject);
    }

    private Sprite duckCircleSprite;

    private void EnsureDuckCircle()
    {
        if (duckCircleSprite != null) return;
        const int sz   = 32;
        var       tex  = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = sz * 0.5f;
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                float a = Mathf.Clamp01(1f - d / half);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        duckCircleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
    }

    private void OnDestroy()
    {
        if (duckCircleSprite != null) { Destroy(duckCircleSprite.texture); Destroy(duckCircleSprite); }
    }

    private LineRenderer MakeDuckLR(float width, int order)
    {
        var go = new GameObject("_DuckVFX");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width; lr.sortingOrder = order;
        var sh = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);
        return lr;
    }

    protected override void OnUpgraded()
    {
        if (definition == null) return;
        switch (definition.StarLevel)
        {
            case 0:
                blastRadius = 4.5f; blastDamage = 28; lureRadius = 5f; lureDuration = 1.5f;
                leavesBurnZone = false; fragments = false; novaProtocol = false;
                break;
            case 1: lureRadius = 7f; lureDuration = 2f; break;
            case 2: leavesBurnZone = true; burnDuration = 2.5f; burnDamage = 4; break;
            case 3: fragments = true; fragmentCount = 3; break;
            case 4: blastRadius = 5.5f; blastDamage = 42; fragmentCount = 5; break;
            case 5: novaProtocol = true; blastRadius = 6f; blastDamage = 55; break;
        }
    }
}
