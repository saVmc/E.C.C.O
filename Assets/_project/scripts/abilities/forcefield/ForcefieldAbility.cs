using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ForcefieldAbility : Ability
{
    public static event Action<float, float> OnShieldChanged;

    // Per-star values set in OnUpgraded()
    private float timerDuration       = 6f;
    private int   maxShieldHP         = 10;
    private float novaBaseRadius      = 3.5f;
    private float novaRadiusPerCharge = 0.06f;
    private float novaDamagePerCharge = 0.35f;
    private int   novaBaseDamage      = 8;
    private const float maxChargeCap  = 60f;    // VFX power scaling only
    private bool  novaStuns           = false;
    private bool  canEarlyRelease     = false;
    private bool  overloadMode        = false;
    private int   starLevel           = 0;

    private bool  fieldActive;
    private float timerRemaining;
    private int   currentShieldHP;
    private bool  shieldBrokenOnce;
    private bool  subscribed;

    private LineRenderer   timerRing;
    private LineRenderer   chargeRing;
    private SpriteRenderer playerSR;
    private Coroutine      fieldCoroutine;

    private Material additiveMat;   // additive-blended material for glow effect
    private Sprite   flashSprite;   // procedural soft circle

    private const int   RingPoints   = 64;
    private const float TimerRadius  = 1.55f;
    private const float ChargeRadius = 1.15f;

    // Stores a pair of LineRenderers (thin bright core + wide dim glow)
    // and the world-space endpoints so we can animate them shooting outward.
    private sealed class GlowLine
    {
        public LineRenderer core, glow;
        public Vector3 from, to;
    }

    private void Awake()  => playerSR = GetComponentInParent<SpriteRenderer>();
    private void Start()  => TrySubscribe();

    private void OnDisable()
    {
        Unsubscribe();
        EndField(release: false);
    }

    private void OnDestroy()
    {
        if (additiveMat  != null) Destroy(additiveMat);
        if (flashSprite  != null) { Destroy(flashSprite.texture); Destroy(flashSprite); }
    }

    // ─── Subscription ─────────────────────────────────────────────────────────

    private void TrySubscribe()
    {
        if (subscribed || PlayerHealth.Instance == null) return;
        PlayerHealth.Instance.OnDamaged += AbsorbDamage;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || PlayerHealth.Instance == null) return;
        PlayerHealth.Instance.OnDamaged -= AbsorbDamage;
        subscribed = false;
    }

    // ─── Activation ───────────────────────────────────────────────────────────

    protected override void Activate()
    {
        TrySubscribe();
        if (fieldActive && canEarlyRelease) { EndField(release: true); return; }
        if (fieldActive) return;
        if (fieldCoroutine != null) StopCoroutine(fieldCoroutine);
        fieldCoroutine = StartCoroutine(FieldRoutine());
    }

    private IEnumerator FieldRoutine()
    {
        fieldActive      = true;
        timerRemaining   = timerDuration;
        currentShieldHP  = maxShieldHP;
        shieldBrokenOnce = false;

        EMPRingEffect.Spawn(transform.position, 1.8f, 0.3f);
        if (playerSR != null) StartCoroutine(FlashPlayer(new Color(0.3f, 0.9f, 1f, 1f), 0.2f));
        EnsureRings();
        FireShieldEvent();

        while (timerRemaining > 0f && fieldActive)
        {
            timerRemaining -= Time.deltaTime;
            UpdateRings();
            FireShieldEvent();
            yield return null;
        }
        if (fieldActive) EndField(release: false);
    }

    // ─── Damage absorption ────────────────────────────────────────────────────

    private void AbsorbDamage(int amount)
    {
        if (!fieldActive || amount <= 0) return;

        int absorbed = Mathf.Min(amount, currentShieldHP);
        if (absorbed <= 0) return;

        currentShieldHP -= absorbed;
        PlayerHealth.Instance?.Heal(absorbed);
        EMPRingEffect.Spawn(transform.position, 0.9f, 0.15f);
        FireShieldEvent();
        UpdateRings();

        if (currentShieldHP <= 0)
            HandleShieldBroken();
    }

    private void HandleShieldBroken()
    {
        // ★5 overload: refresh to half shield once instead of breaking
        if (overloadMode && !shieldBrokenOnce)
        {
            shieldBrokenOnce = true;
            currentShieldHP  = maxShieldHP / 2;
            timerRemaining   = timerDuration;
            EMPRingEffect.Spawn(transform.position, 2.5f, 0.4f);
            if (playerSR != null) StartCoroutine(FlashPlayer(new Color(1f, 0.6f, 0f, 1f), 0.15f));
            FireShieldEvent();
            UpdateRings();
            return;
        }

        float novaCharge = starLevel >= 1 ? (float)starLevel / 5f * maxChargeCap : 0f;
        EndField(release: false);
        if (novaCharge > 0f)
            StartCoroutine(NovaSequence(transform.position, novaCharge));
    }

    // ─── End field ────────────────────────────────────────────────────────────

    private void EndField(bool release)
    {
        if (!fieldActive) return;
        fieldActive     = false;
        currentShieldHP = 0;
        timerRemaining  = 0f;
        DestroyRings();
        FireShieldEvent();

        // ★4+ early release fires a weaker nova
        if (release && canEarlyRelease)
        {
            float novaCharge = (float)starLevel / 5f * maxChargeCap * 0.65f;
            StartCoroutine(NovaSequence(transform.position, novaCharge));
        }
    }

    // ─── Nova ─────────────────────────────────────────────────────────────────

    private IEnumerator NovaSequence(Vector2 center, float charge)
    {
        float radius = novaBaseRadius + charge * novaRadiusPerCharge;
        int   damage = novaBaseDamage + Mathf.RoundToInt(charge * novaDamagePerCharge);
        float power  = Mathf.Clamp01(charge / maxChargeCap);

        ApplyNovaDamage(center, radius, damage);
        yield return StartCoroutine(NovaVFX(center, radius, power));
    }

    private void ApplyNovaDamage(Vector2 center, float radius, int damage)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            e.TakeDamage(damage);
            if (novaStuns) e.ApplySlow(0f, 1.8f);
            Vector2 d = ((Vector2)e.transform.position - center).normalized;
            if (d.sqrMagnitude < 0.01f) d = UnityEngine.Random.insideUnitCircle.normalized;
            e.ForceKnockback(d, 14f);
        }
    }

    // ─── NOVA VFX ─────────────────────────────────────────────────────────────
    //
    //  Each star level adds a new cinematic layer:
    //   ★0  void tear + 6 glow lines shoot out + 1 ring + shake
    //   ★1  more lines, 2 rings, cyan tint
    //   ★2  screen bloom flash, expanding shockwave ring, 3 rings
    //   ★3  purple shift, second burst wave, 4 rings, stun flash
    //   ★4  0.07x slow-mo freeze + snap, 5 rings, heavy shake
    //   ★5  SUPERNOVA: 0.02x hard freeze, spinning space rift, 3 burst waves,
    //       7 rings, double shockwave, screen bloom × 2, rumble shake

    private IEnumerator NovaVFX(Vector2 center, float radius, float power)
    {
        EnsureAdditiveMat();
        Camera cam = Camera.main;

        int   lineCount = 6  + Mathf.RoundToInt(power * 26f);   // 6 → 32
        int   ringCount = 1  + Mathf.RoundToInt(power * 6f);    // 1 → 7
        float shakeMag  = 0.05f + power * 0.30f;
        float shakeDur  = 0.35f + power * 0.90f;

        // ─ ★4–5: cinematic slow-mo freeze before burst ─
        if (starLevel >= 4)
        {
            float slowScale = starLevel >= 5 ? 0.02f : 0.07f;
            float slowReal  = starLevel >= 5 ? 0.22f : 0.10f;
            Time.timeScale  = slowScale;
            if (playerSR != null) playerSR.color = Color.white * 10f;
            yield return new WaitForSecondsRealtime(slowReal);
            Time.timeScale = 1f;
            if (playerSR != null) playerSR.color = Color.white;
        }

        // Bright player flash on burst
        if (playerSR != null) StartCoroutine(FlashPlayer(Color.white * 5f, 0.05f));

        // ─ Dark void tear at center (★0+) ─
        StartCoroutine(VoidTear(center, radius * (0.25f + power * 0.20f), 0.55f + power * 0.35f, power));

        // ─ ★2+: screen bloom ─
        if (starLevel >= 2)
            StartCoroutine(ScreenBloom(center, radius, power));

        // ─ Primary burst lines (shoot outward) ─
        List<GlowLine> burst1 = CreateGlowLines(center, lineCount, radius, power);
        StartCoroutine(AnimateLinesGrow(burst1, 0.16f));

        // ─ Fast expanding shockwave ring ─
        StartCoroutine(ExpandingShockwave(center, radius * (1.2f + power * 0.6f), 0.38f - power * 0.09f, power));

        // ─ Staggered decay rings ─
        for (int i = 0; i < ringCount; i++)
        {
            float rd = i * (0.065f - power * 0.015f);
            float rr = radius * (0.15f + i * 0.20f);
            float rt = 0.32f + i * 0.12f;
            StartCoroutine(DelayedRing(center, rr, rt, rd, power, i));
        }

        // ─ Camera shake ─
        if (cam != null)
            StartCoroutine(ShakeCamera(cam, shakeMag, shakeDur, withRumble: starLevel >= 5));

        yield return new WaitForSecondsRealtime(0.18f);

        // ─ ★3+: second burst wave (offset) ─
        if (starLevel >= 3)
        {
            int   c2 = Mathf.Max(1, lineCount / 2);
            float o2 = Mathf.PI / Mathf.Max(1f, lineCount);
            List<GlowLine> burst2 = CreateGlowLines(center, c2, radius * 0.65f, power * 0.75f, o2);
            StartCoroutine(AnimateLinesGrow(burst2, 0.12f));
            yield return new WaitForSecondsRealtime(0.12f);
            StartCoroutine(FadeGlowLines(burst2, 0.45f));
        }

        // ─ ★5: spinning space rift + third burst + outer shockwave ─
        if (starLevel >= 5)
        {
            StartCoroutine(SpinningRift(center, radius * 0.55f, 1.0f));
            StartCoroutine(ScreenBloom(center, radius * 1.5f, 1f));
            StartCoroutine(ExpandingShockwave(center, radius * 2.5f, 0.70f, 1f));

            yield return new WaitForSecondsRealtime(0.09f);
            int   c3 = Mathf.Max(1, lineCount / 3);
            float o3 = Mathf.PI * 0.67f / Mathf.Max(1f, lineCount);
            List<GlowLine> burst3 = CreateGlowLines(center, c3, radius * 0.50f, 1f, o3);
            StartCoroutine(AnimateLinesGrow(burst3, 0.10f));
            yield return new WaitForSecondsRealtime(0.10f);
            StartCoroutine(FadeGlowLines(burst3, 0.60f));
        }

        yield return StartCoroutine(FadeGlowLines(burst1, 0.45f + power * 0.55f));
    }

    // ─── Void tear — dark glowing disc that expands then collapses ────────────

    private IEnumerator VoidTear(Vector2 center, float maxRadius, float duration, float power)
    {
        EnsureFlashSprite();
        GameObject     go = new GameObject("_VoidTear");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = flashSprite;
        sr.sortingOrder = 29;
        sr.material     = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        go.transform.position = new Vector3(center.x, center.y, 0f);

        // Deep void: near-black with a tint of the energy colour
        Color voidCol = Color.Lerp(new Color(0f, 0.05f, 0.20f, 1f),
                                   new Color(0.10f, 0f, 0.25f, 1f), power);

        // Phase 1 – Expand
        float expandDur = duration * 0.30f;
        float t = 0f;
        while (t < expandDur)
        {
            t += Time.unscaledDeltaTime;
            float f    = Mathf.Sqrt(t / expandDur);                    // fast start
            float size = Mathf.Lerp(0f, maxRadius * 2f, f);
            go.transform.localScale = Vector3.one * size;
            sr.color = new Color(voidCol.r, voidCol.g, voidCol.b, Mathf.Lerp(0f, 1f, f));
            yield return null;
        }

        // Phase 2 – Hold + pulse + collapse
        float collapseDur = duration * 0.70f;
        t = 0f;
        while (t < collapseDur)
        {
            t += Time.unscaledDeltaTime;
            float f     = t / collapseDur;
            float pulse = 1f + Mathf.Sin(f * Mathf.PI * 5f) * 0.12f * (1f - f);
            float size  = Mathf.Lerp(maxRadius * 2f, 0f, f * f) * pulse;
            go.transform.localScale = Vector3.one * Mathf.Max(0f, size);
            sr.color = new Color(voidCol.r, voidCol.g, voidCol.b, Mathf.Lerp(1f, 0f, f));
            yield return null;
        }
        Destroy(go.GetComponent<SpriteRenderer>().material);
        Destroy(go);
    }

    // ─── Screen bloom ─────────────────────────────────────────────────────────

    private IEnumerator ScreenBloom(Vector2 center, float radius, float power)
    {
        EnsureFlashSprite();
        GameObject     go = new GameObject("_NovaBloom");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = flashSprite;
        sr.sortingOrder = 31;

        // Use additive material if available so it actually glows, not just overlays
        Material bloomMat = additiveMat != null
            ? additiveMat
            : new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        sr.material = bloomMat;

        float size = radius * (2.2f + power * 4.0f);
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * size;

        Color col = Color.Lerp(new Color(0.4f, 1f, 1f, 0.90f),
                               new Color(1f,   0.5f, 1f, 0.90f), power);
        sr.color = col;

        float dur = 0.18f + power * 0.28f;
        float t   = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(col.a, 0f, t / dur);
            sr.color = new Color(col.r, col.g, col.b, a);
            yield return null;
        }
        if (bloomMat != additiveMat) Destroy(bloomMat);
        Destroy(go);
    }

    // ─── Expanding shockwave ring ─────────────────────────────────────────────

    private IEnumerator ExpandingShockwave(Vector2 center, float maxRadius, float duration, float power)
    {
        Color col = Color.Lerp(new Color(0.3f, 1f, 1f, 1f), new Color(0.9f, 0.2f, 1f, 1f), power);

        // Two rings: thick outer glow + thin bright core
        LineRenderer outerLR = MakeLR(0.30f + power * 0.20f, 0f, 26);
        LineRenderer coreLR  = MakeLR(0.06f,                  0f, 27);
        outerLR.loop = coreLR.loop = true;
        outerLR.positionCount = coreLR.positionCount = 48;
        Color outerCol = new Color(col.r, col.g, col.b, 0.35f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float frac = elapsed / duration;
            float r    = Mathf.Lerp(0.05f, maxRadius, Mathf.Sqrt(frac));
            float a    = Mathf.Lerp(1f, 0f, frac * frac);

            coreLR.startWidth = coreLR.endWidth = Mathf.Lerp(0.06f, 0f, frac);
            outerLR.startWidth = outerLR.endWidth = Mathf.Lerp(0.30f + power * 0.20f, 0f, frac);

            Color cc = new Color(col.r,      col.g,      col.b,      a);
            Color gc = new Color(outerCol.r, outerCol.g, outerCol.b, a * 0.35f);
            coreLR.startColor  = coreLR.endColor  = cc;
            outerLR.startColor = outerLR.endColor = gc;

            for (int i = 0; i < 48; i++)
            {
                float ang = (i / 48f) * Mathf.PI * 2f;
                Vector3 p = new Vector3(center.x + Mathf.Cos(ang) * r,
                                        center.y + Mathf.Sin(ang) * r, 0f);
                coreLR.SetPosition(i, p);
                outerLR.SetPosition(i, p);
            }
            yield return null;
        }
        if (coreLR  != null) Destroy(coreLR.gameObject);
        if (outerLR != null) Destroy(outerLR.gameObject);
    }

    // ─── Spinning space rift (★5 only) ────────────────────────────────────────
    // Short energised spokes that spin rapidly around the void tear centre.

    private IEnumerator SpinningRift(Vector2 center, float radius, float duration)
    {
        const int spokeCount = 6;
        LineRenderer[] spokes = new LineRenderer[spokeCount];
        float[] angles = new float[spokeCount];
        for (int i = 0; i < spokeCount; i++)
        {
            angles[i]  = (i / (float)spokeCount) * Mathf.PI * 2f;
            spokes[i]  = MakeLR(0.06f, 0f, 28);
        }
        Color sc = new Color(0.9f, 0.4f, 1f, 0.9f);

        float spinRads = 240f * Mathf.Deg2Rad;   // 240 deg/s
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade   = 1f - (elapsed / duration);
            float inner  = radius * 0.18f;
            float outer  = radius * (0.4f + fade * 0.6f);

            for (int i = 0; i < spokeCount; i++)
            {
                if (spokes[i] == null) continue;
                angles[i] += spinRads * Time.unscaledDeltaTime;
                float a  = angles[i];
                Vector2 p0 = center + new Vector2(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner);
                Vector2 p1 = center + new Vector2(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer);
                spokes[i].SetPosition(0, p0);
                spokes[i].SetPosition(1, p1);
                spokes[i].startColor = new Color(sc.r, sc.g, sc.b, fade * 0.9f);
                spokes[i].endColor   = new Color(sc.r, sc.g, sc.b, 0f);
                spokes[i].startWidth = 0.06f * fade;
            }
            yield return null;
        }
        foreach (LineRenderer lr in spokes)
            if (lr != null) Destroy(lr.gameObject);
    }

    // ─── Glow-line set: each line is a bright core + wide dim glow ────────────

    private List<GlowLine> CreateGlowLines(Vector2 center, int count, float maxLen, float power, float offsetAngle = 0f)
    {
        var list = new List<GlowLine>(count);

        // Core: bright, thin — shifts from electric cyan to arcane purple with power
        Color coreCol = Color.Lerp(new Color(0.6f, 1f,    1f,    1f),
                                   new Color(1f,   0.35f, 1f,    1f), power);
        // Glow: wide, transparent — same hue but dim
        Color glowCol = Color.Lerp(new Color(0f,   0.5f,  1f,  0.28f),
                                   new Color(0.7f, 0f,    1f,  0.22f), power);

        for (int i = 0; i < count; i++)
        {
            float angle = offsetAngle + (i / (float)count) * Mathf.PI * 2f;
            float len   = maxLen * UnityEngine.Random.Range(0.48f, 1f);
            float coreW = UnityEngine.Random.Range(0.025f, 0.042f + power * 0.04f);
            float glowW = coreW * 5f;

            Vector2 dir  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 from = center + dir * 0.18f;
            Vector3 to   = center + dir * len;

            // Both start at origin — AnimateLinesGrow will extend them outward
            LineRenderer core = MakeLR(coreW, 0f, 28);
            core.SetPosition(0, from);
            core.SetPosition(1, from);
            core.startColor = coreCol;
            core.endColor   = new Color(coreCol.r, coreCol.g, coreCol.b, 0f);

            LineRenderer glow = MakeLR(glowW, 0f, 26);
            glow.SetPosition(0, from);
            glow.SetPosition(1, from);
            glow.startColor = glowCol;
            glow.endColor   = new Color(glowCol.r, glowCol.g, glowCol.b, 0f);

            list.Add(new GlowLine { core = core, glow = glow, from = from, to = to });
        }
        return list;
    }

    // Lines extend from their origin to their target over `duration` seconds.
    private IEnumerator AnimateLinesGrow(List<GlowLine> lines, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float frac = Mathf.SmoothStep(0f, 1f, t / duration);
            foreach (GlowLine gl in lines)
            {
                if (gl.core == null) continue;
                Vector3 tip = Vector3.LerpUnclamped(gl.from, gl.to, frac);
                gl.core.SetPosition(1, tip);
                gl.glow.SetPosition(1, tip);
            }
            yield return null;
        }
    }

    private IEnumerator FadeGlowLines(List<GlowLine> lines, float duration)
    {
        // Capture initial alpha for smooth fade
        float[] coreA = new float[lines.Count];
        float[] glowA = new float[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].core == null) continue;
            coreA[i] = lines[i].core.startColor.a;
            glowA[i] = lines[i].glow.startColor.a;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float frac = t / duration;
            for (int i = 0; i < lines.Count; i++)
            {
                GlowLine gl = lines[i];
                if (gl.core == null) continue;

                Color cs = gl.core.startColor;
                gl.core.startColor = new Color(cs.r, cs.g, cs.b, Mathf.Lerp(coreA[i], 0f, frac));
                gl.core.startWidth = Mathf.Lerp(gl.core.startWidth, 0f, frac * 0.4f);

                Color gs = gl.glow.startColor;
                gl.glow.startColor = new Color(gs.r, gs.g, gs.b, Mathf.Lerp(glowA[i], 0f, frac));
                gl.glow.startWidth = Mathf.Lerp(gl.glow.startWidth, 0f, frac * 0.4f);
            }
            yield return null;
        }
        foreach (GlowLine gl in lines)
        {
            if (gl.core != null) Destroy(gl.core.gameObject);
            if (gl.glow != null) Destroy(gl.glow.gameObject);
        }
    }

    // ─── Staggered decay rings ────────────────────────────────────────────────

    private IEnumerator DelayedRing(Vector2 center, float radius, float dur, float delay, float power, int index)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        Color ringCol = (index % 2 == 0)
            ? Color.Lerp(new Color(0.2f, 1f,    1f,    0.85f), new Color(0.85f, 0.2f, 1f, 0.85f), power)
            : Color.Lerp(new Color(1f,   0.95f, 0.25f, 0.75f), new Color(1f,    0.3f, 1f, 0.75f), power);

        EMPRingEffect.Spawn(center, radius, dur);

        // Glowing ring = thick outer glow + thin core
        LineRenderer rCore = MakeLR(0.045f + power * 0.04f, 0f, 23);
        LineRenderer rGlow = MakeLR(0.18f  + power * 0.10f, 0f, 21);
        SetRingPositions(rCore, center, radius * 0.95f);
        SetRingPositions(rGlow, center, radius * 0.95f);
        rCore.startColor = rCore.endColor = ringCol;
        Color glowRingCol = new Color(ringCol.r, ringCol.g, ringCol.b, ringCol.a * 0.30f);
        rGlow.startColor = rGlow.endColor = glowRingCol;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / dur);
            rCore.startColor = rCore.endColor = new Color(ringCol.r,     ringCol.g,     ringCol.b,     ringCol.a * a);
            rGlow.startColor = rGlow.endColor = new Color(glowRingCol.r, glowRingCol.g, glowRingCol.b, glowRingCol.a * a);
            yield return null;
        }
        if (rCore != null) Destroy(rCore.gameObject);
        if (rGlow != null) Destroy(rGlow.gameObject);
    }

    // ─── Camera shake (sinusoidal + optional random rumble) ───────────────────

    private IEnumerator ShakeCamera(Camera cam, float magnitude, float duration, bool withRumble = false)
    {
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - elapsed / duration;
            float mag   = magnitude * decay * decay;
            float x = Mathf.Sin(elapsed * 43f) * mag;
            float y = Mathf.Cos(elapsed * 31f) * mag;
            if (withRumble)
            {
                float rm = magnitude * 0.38f * decay;
                x += UnityEngine.Random.Range(-rm, rm);
                y += UnityEngine.Random.Range(-rm, rm);
            }
            cam.transform.position = basePos + new Vector3(x, y, 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    // ─── Material / sprite helpers ────────────────────────────────────────────

    // Tries to create an additive-blended material so lines actually glow.
    // Additive blending means overlapping bright lines accumulate into white hotspots.
    private void EnsureAdditiveMat()
    {
        if (additiveMat != null) return;

        // Particles/Additive is a legacy built-in shader that works in both
        // Built-in RP and URP (as a fallback). It blends: src * srcAlpha + dst.
        Shader sh = Shader.Find("Particles/Additive")
                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Sprites/Default");
        if (sh != null) additiveMat = new Material(sh);
    }

    private void EnsureFlashSprite()
    {
        if (flashSprite != null) return;
        const int size = 64;
        Texture2D tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                float a = Mathf.Clamp01(1f - d / half);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        flashSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private LineRenderer MakeLR(float startW, float endW, int order)
    {
        EnsureAdditiveMat();
        GameObject   go = new GameObject("_NVfx");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = startW;
        lr.endWidth      = endW;
        lr.sortingOrder  = order;
        if (additiveMat != null) lr.material = additiveMat;
        return lr;
    }

    // Sets ring geometry without touching colour — caller sets colour after.
    private void SetRingPositions(LineRenderer lr, Vector2 center, float radius)
    {
        lr.positionCount = 48;
        lr.loop          = true;
        for (int i = 0; i < 48; i++)
        {
            float a = (i / 48f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius,
                                          center.y + Mathf.Sin(a) * radius, 0f));
        }
    }

    // ─── Active-field arc rings ────────────────────────────────────────────────

    private void EnsureRings()
    {
        if (timerRing  == null) timerRing  = MakeLocalRing(TimerRadius,  new Color(1f, 0.35f, 0.05f, 1f), 0.10f, 20);
        if (chargeRing == null) chargeRing = MakeLocalRing(ChargeRadius, new Color(1f, 0.95f, 0.1f,  1f), 0.08f, 18);
    }

    private void UpdateRings()
    {
        if (timerRing == null || chargeRing == null) return;

        float timerFrac  = timerDuration > 0f ? Mathf.Clamp01(timerRemaining / timerDuration) : 0f;
        float shieldFrac = maxShieldHP > 0 ? Mathf.Clamp01((float)currentShieldHP / maxShieldHP) : 0f;

        float timerPulse  = timerFrac  < 0.25f ? 0.10f + Mathf.Sin(Time.time * 18f) * 0.04f : 0.10f;
        float shieldPulse = shieldFrac < 0.30f ? 0.08f + Mathf.Sin(Time.time * 14f) * 0.03f : 0.08f;

        Color timerCol  = Color.Lerp(new Color(1f, 0.1f, 0.05f, 1f), new Color(1f, 0.9f, 0.1f, 1f), timerFrac);
        Color shieldCol = Color.Lerp(new Color(0f, 0.4f, 1f, 0.7f), new Color(0.1f, 0.95f, 1f, 1f), shieldFrac);

        SetArc(timerRing,  TimerRadius,  timerFrac,  timerCol,  timerPulse,  Mathf.PI * 0.5f, clockwise: true);
        SetArc(chargeRing, ChargeRadius, shieldFrac, shieldCol, shieldPulse, Mathf.PI * 0.5f, clockwise: false);
    }

    private void SetArc(LineRenderer lr, float radius, float fraction, Color color, float width, float startAngle, bool clockwise)
    {
        if (lr == null) return;
        fraction = Mathf.Clamp01(fraction);
        int points = Mathf.Max(2, Mathf.RoundToInt(fraction * RingPoints));
        lr.positionCount = points;
        lr.startWidth    = width;
        lr.endWidth      = width * 0.6f;
        lr.startColor    = color;
        lr.endColor      = new Color(color.r, color.g, color.b, color.a * 0.3f);
        float dir = clockwise ? -1f : 1f;
        for (int i = 0; i < points; i++)
        {
            float t = points > 1 ? i / (float)(points - 1) : 0f;
            float a = startAngle + dir * t * fraction * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    private void DestroyRings()
    {
        if (timerRing  != null) { Destroy(timerRing.gameObject);  timerRing  = null; }
        if (chargeRing != null) { Destroy(chargeRing.gameObject); chargeRing = null; }
    }

    private LineRenderer MakeLocalRing(float radius, Color color, float width, int sortOrder)
    {
        GameObject   go = new GameObject("_ForceRing");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        lr.useWorldSpace  = false;
        lr.loop           = false;
        lr.positionCount  = RingPoints;
        lr.startWidth     = width;
        lr.endWidth       = width * 0.6f;
        lr.startColor     = color;
        lr.endColor       = new Color(color.r, color.g, color.b, 0.3f);
        lr.sortingOrder   = sortOrder;
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (sh != null) lr.material = new Material(sh);
        return lr;
    }

    private IEnumerator FlashPlayer(Color flashColor, float duration)
    {
        if (playerSR == null) yield break;
        Color orig = playerSR.color;
        playerSR.color = flashColor;
        yield return new WaitForSecondsRealtime(duration);
        if (playerSR != null) playerSR.color = orig;
    }

    private void FireShieldEvent()
    {
        OnShieldChanged?.Invoke(currentShieldHP, maxShieldHP);
    }

    // ─── Star-level upgrade config ────────────────────────────────────────────
    //
    //  Nova damage caps (full charge, post-nerf):
    //    ★0   8 + 30 * 0.35 ≈  19
    //    ★1  10 + 35 * 0.45 ≈  26
    //    ★2  13 + 40 * 0.55 ≈  35
    //    ★3  16 + 45 * 0.65 ≈  45  + stun
    //    ★4  20 + 50 * 0.75 ≈  58  + early release
    //    ★5  26 + 60 * 0.90 ≈  80  + overload + bigger AoE

    protected override void OnUpgraded()
    {
        if (definition == null) return;
        starLevel = definition.StarLevel;
        switch (starLevel)
        {
            case 0:
                maxShieldHP = 10; timerDuration = 6f; novaBaseRadius = 3.5f;
                novaBaseDamage = 8; novaDamagePerCharge = 0.35f; novaRadiusPerCharge = 0.06f;
                novaStuns = false; canEarlyRelease = false; overloadMode = false;
                break;
            case 1:
                maxShieldHP = 15; timerDuration = 7f;
                novaBaseDamage = 12; novaDamagePerCharge = 0.45f;
                break;
            case 2:
                maxShieldHP = 20;
                novaBaseDamage = 16; novaDamagePerCharge = 0.55f; novaRadiusPerCharge = 0.09f;
                break;
            case 3:
                novaStuns = true; maxShieldHP = 25; timerDuration = 8f;
                novaBaseDamage = 20; novaDamagePerCharge = 0.65f;
                break;
            case 4:
                canEarlyRelease = true; maxShieldHP = 30; timerDuration = 8f;
                novaBaseDamage = 25; novaDamagePerCharge = 0.75f;
                break;
            case 5:
                overloadMode = true; maxShieldHP = 40; timerDuration = 9f; novaBaseRadius = 4.5f;
                novaBaseDamage = 32; novaDamagePerCharge = 0.90f;
                break;
        }
    }
}
