using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public sealed class SingularityRiftAbility : Ability
{
    // Per-star gameplay values
    private float pullRadius            = 7f;
    private float pullForce             = 6f;
    private float duration              = 4.5f;
    private float absorbPct             = 0.20f;  // <1: fraction of max HP/sec at center; >=1: instant kill
    private float collapsePct           = 0.05f;  // fraction of enemy max HP on detonation
    private float collapsePerAbsorbPct  = 0.003f; // bonus fraction per absorbed enemy (tiny at low stars)
    private float collapseRadius        = 3.0f;
    private bool  hasOrbitalPhase   = false;
    private bool  scalingBlast      = false;
    private bool  eventHorizon      = false;
    private float execThreshold     = 0.4f;
    private bool  absorbHeals       = false;
    private int   starLevel         = 0;

    private bool           riftActive;
    private bool           isCinematic;
    private Rigidbody2D    rb;
    private SpriteRenderer playerSR;
    private PlayerMovement movement;
    private Material       additiveMat;
    private Sprite         circleSprite;

    private sealed class OrbParticle
    {
        public LineRenderer lr;
        public float angle, baseRadius, angSpeed, tilt, twinkleSpd;
        public Color col;
    }

    private void Awake()
    {
        rb       = GetComponentInParent<Rigidbody2D>();
        playerSR = GetComponentInParent<SpriteRenderer>();
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void OnDisable()
    {
        // Safety: restore time if cinematic is interrupted
        if (isCinematic || riftActive)
        {
            Time.timeScale = 1f;
            riftActive     = false;
            isCinematic    = false;
        }
    }

    private void OnDestroy()
    {
        if (additiveMat  != null) Destroy(additiveMat);
        if (circleSprite != null) { Destroy(circleSprite.texture); Destroy(circleSprite); }
    }

    // ─── Activate ────────────────────────────────────────────────────────────

    protected override void Activate()
    {
        if (riftActive || isCinematic) return;

        Vector2 origin   = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 screen   = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
        Vector2 dir      = mousePos - origin;
        Vector2 center   = origin + dir.normalized * Mathf.Min(dir.magnitude, 14f);

        if (eventHorizon)
            StartCoroutine(CinematicEventHorizon(center));
        else
            StartCoroutine(RiftRoutine(center));
    }

    // ─── Standard pull routine (★0-4) ────────────────────────────────────────

    private IEnumerator RiftRoutine(Vector2 center)
    {
        riftActive = true;
        EnsureAdditiveMat();
        EnsureCircleSprite();

        float power = starLevel / 5f;

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer  = LayerMask.NameToLayer("Enemy");

        yield return StartCoroutine(FormationVFX(center, power));

        StartCoroutine(VoidCoreVFX(center, power));
        StartCoroutine(SpiralArmsVFX(center, power));
        StartCoroutine(AccretionDiskVFX(center, power));
        StartCoroutine(EventRingsVFX(center, power));

        int   enemiesAbsorbed = 0;
        float elapsed         = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bool orbital = hasOrbitalPhase && t > 0.3f && t < 0.72f;

            foreach (Collider2D c in Physics2D.OverlapCircleAll(center, pullRadius))
            {
                Enemy e = c.GetComponentInParent<Enemy>();
                if (e == null || e.IsDead) continue;
                float d = Vector2.Distance(e.transform.position, center);

                if (d < 0.6f)
                {
                    if (absorbPct > 0f)
                    {
                        int absorbAmt = absorbPct >= 1f
                            ? Mathf.RoundToInt(e.MaxHealth)
                            : Mathf.Max(1, Mathf.RoundToInt(e.MaxHealth * absorbPct * Time.deltaTime));
                        e.TakeDamage(absorbAmt);
                        if (absorbHeals && !e.IsDead) PlayerHealth.Instance?.Heal(1);
                    }
                    enemiesAbsorbed++;
                    EMPRingEffect.Spawn(center, 0.5f, 0.12f);
                    continue;
                }
                if (orbital && d > 1.5f)
                {
                    Vector2 toC = ((Vector2)e.transform.position - center).normalized;
                    e.PullToward(center, pullForce * 0.25f);
                    Rigidbody2D erb = e.GetComponent<Rigidbody2D>();
                    if (erb != null) erb.linearVelocity += new Vector2(-toC.y, toC.x) * 4f * Time.deltaTime;
                }
                else e.PullToward(center, pullForce);
            }
            yield return null;
        }

        riftActive = false;
        yield return new WaitForSecondsRealtime(0.07f);
        yield return StartCoroutine(CollapseVFX(center, enemiesAbsorbed, power));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ★5 CINEMATIC — EVENT HORIZON
    //  A full cutscene. Reality tears apart. Everything falls into one point.
    // ═══════════════════════════════════════════════════════════════════════════

    private IEnumerator CinematicEventHorizon(Vector2 center)
    {
        isCinematic = true;
        riftActive  = true;
        EnsureAdditiveMat();
        EnsureCircleSprite();

        // ─ Freeze player ─
        if (movement != null) movement.enabled = false;
        if (rb       != null) rb.linearVelocity = Vector2.zero;
        PlayerHealth.Instance?.SetInvincible(true);

        // ─ Camera & UI setup ─
        Camera             cam      = Camera.main;
        PixelPerfectCamera pixelCam = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;
        if (pixelCam != null) pixelCam.enabled = false;

        Canvas[] canvases     = FindObjectsByType<Canvas>();
        bool[]   canvasStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
            { canvasStates[i] = canvases[i].enabled; canvases[i].enabled = false; }

        Vector3 camOrigPos  = cam != null ? cam.transform.position : Vector3.back * 10f;
        float   camOrigSize = cam != null ? cam.orthographicSize : 5f;
        float   camWideSize = camOrigSize * 1.7f;                              // zoom OUT for establishing shot
        Vector3 camCtrPos   = new Vector3(center.x, center.y, camOrigPos.z);  // look at the rift

        SpriteRenderer blackout = CreateBlackoutQuad(cam, center);

        // ─ Pre-tremor: three warning pulses — the universe feels something ─
        yield return new WaitForSecondsRealtime(0.05f);
        EMPRingEffect.Spawn(center, pullRadius * 0.25f, 0.06f);
        if (cam != null) StartCoroutine(ShakeCam(cam, 0.025f, 0.18f));
        yield return new WaitForSecondsRealtime(0.12f);
        EMPRingEffect.Spawn(center, pullRadius * 0.55f, 0.09f);
        yield return new WaitForSecondsRealtime(0.10f);
        EMPRingEffect.Spawn(center, pullRadius * 0.90f, 0.13f);
        if (cam != null) StartCoroutine(ShakeCam(cam, 0.04f, 0.25f));
        yield return new WaitForSecondsRealtime(0.08f);

        // ─────────────────────────────────────────────────────────────────────
        //  ACT I — THE WORLD NOTICES (0.7s real)
        //  Camera pulls back. Space darkens. Something is wrong.
        // ─────────────────────────────────────────────────────────────────────

        float e = 0f;
        while (e < 0.70f)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / 0.70f);
            if (cam != null)
            {
                // Pan toward rift while zooming out
                cam.transform.position = Vector3.Lerp(camOrigPos, camCtrPos + (camOrigPos - camCtrPos) * 0.25f, t * 0.5f);
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(camOrigSize, camWideSize, t);
            }
            // Deep space: not black, a dark violet-blue
            if (blackout != null) blackout.color = new Color(0.01f, 0f, 0.04f, t * 0.82f);
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ACT II — REALITY FRACTURES (0.15x slow-mo, 0.7s real)
        //  Glass-crack lines spread from the singularity point. Space tears.
        // ─────────────────────────────────────────────────────────────────────

        Time.timeScale = 0.15f;

        // 14 fracture cracks — longer and more dramatic than the standard formation
        var cracks = new List<LineRenderer>(14);
        for (int i = 0; i < 14; i++)
        {
            float ang = (i / 14f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.22f, 0.22f);
            float len = pullRadius * UnityEngine.Random.Range(0.30f, 0.80f);
            Vector2 d   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 mid = center + d * len * 0.45f + (Vector2)(UnityEngine.Random.insideUnitCircle * 0.50f);
            Vector2 tip = center + d * len;

            LineRenderer lr = MakeLR(0.045f, 0f, 943);
            lr.positionCount = 3;
            lr.SetPosition(0, center); lr.SetPosition(1, mid); lr.SetPosition(2, tip);
            lr.startColor = new Color(0.85f, 0.65f, 1f, 0f);
            lr.endColor   = new Color(0.50f, 0.25f, 1f, 0f);
            cracks.Add(lr);
        }

        // Cracks flash in over 0.35s real
        StartCoroutine(SpawnBloom(center, pullRadius * 0.55f, new Color(0.45f, 0.1f, 1f, 0.9f), 0.5f));
        float ct = 0f;
        while (ct < 0.35f)
        {
            ct += Time.unscaledDeltaTime;
            float frac = ct / 0.35f;
            foreach (var lr in cracks)
            {
                if (lr == null) continue;
                lr.startColor = new Color(0.85f, 0.65f, 1f, frac);
                lr.endColor   = new Color(0.50f, 0.25f, 1f, frac * 0.18f);
            }
            yield return null;
        }

        // Flicker — cracks pulse like static in glass before the void opens
        foreach (var lr in cracks) { if (lr != null) { lr.startColor = new Color(1f, 0.90f, 1f, 0.12f); lr.endColor = new Color(0.6f, 0.3f, 1f, 0.03f); } }
        StartCoroutine(SpawnBloom(center, pullRadius * 0.16f, new Color(0.95f, 0.85f, 1f, 0.80f), 0.09f));
        yield return new WaitForSecondsRealtime(0.07f);
        foreach (var lr in cracks) { if (lr != null) { lr.startColor = new Color(1f, 0.92f, 1f, 1f); lr.endColor = new Color(0.7f, 0.40f, 1f, 0.38f); } }
        StartCoroutine(SpawnBloom(center, pullRadius * 0.30f, new Color(0.90f, 0.75f, 1f, 1.0f), 0.12f));
        if (cam != null) StartCoroutine(ShakeCam(cam, 0.05f, 0.18f));
        yield return new WaitForSecondsRealtime(0.08f);
        foreach (var lr in cracks) { if (lr != null) { lr.startColor = new Color(0.85f, 0.65f, 1f, 0.75f); lr.endColor = new Color(0.5f, 0.25f, 1f, 0.18f); } }
        yield return new WaitForSecondsRealtime(0.06f);
        foreach (var lr in cracks) { if (lr != null) { lr.startColor = new Color(1f, 0.90f, 1f, 1.0f); lr.endColor = new Color(0.6f, 0.30f, 1f, 0.35f); } }
        StartCoroutine(SpawnBloom(center, pullRadius * 0.42f, new Color(0.85f, 0.70f, 1f, 0.90f), 0.18f));
        yield return new WaitForSecondsRealtime(0.14f);

        StartCoroutine(FadeLinesOut(cracks, 0.55f));
        EMPRingEffect.Spawn(center, pullRadius * 0.50f, 0.22f);
        EMPRingEffect.Spawn(center, pullRadius,         0.38f);

        // ─────────────────────────────────────────────────────────────────────
        //  ACT III — THE VOID OPENS (time ramps 0.15→0.55x, 2.8s real)
        //  The black hole is fully alive. Every enemy is being spaghettified.
        //  Lines of purple energy stretch from each enemy toward the center.
        // ─────────────────────────────────────────────────────────────────────

        StartCoroutine(VoidCoreVFX(center, 1f));
        StartCoroutine(SpiralArmsVFX(center, 1f));
        StartCoroutine(AccretionDiskVFX(center, 1f));
        StartCoroutine(EventRingsVFX(center, 1f));
        StartCoroutine(ChargingPulseVFX(center, 2.8f));
        StartCoroutine(InwardStarfieldVFX(center, pullRadius * 1.4f, 2.8f));

        int enemiesAbsorbed = 0;
        float pullReal      = 2.8f;
        float pullElapsed   = 0f;

        // Track spaghettification lines per enemy instance ID
        var spagLines = new Dictionary<int, LineRenderer>();

        // Camera base for rumble — stored so we can restore cleanly after the loop
        Vector3 actIIICamBase = cam != null ? cam.transform.position : camCtrPos;

        try
        {
            while (pullElapsed < pullReal)
            {
                pullElapsed    += Time.unscaledDeltaTime;
                float tp        = pullElapsed / pullReal;
                Time.timeScale  = Mathf.Lerp(0.15f, 0.55f, tp);

                // Building camera tremor — the singularity shakes reality
                if (cam != null)
                {
                    float ri = Mathf.Lerp(0.012f, 0.085f, tp);
                    cam.transform.position = actIIICamBase + new Vector3(
                        UnityEngine.Random.Range(-ri, ri),
                        UnityEngine.Random.Range(-ri, ri), 0f);
                }

                // Make blackout breathe: pulses slightly as enemies are consumed
                if (blackout != null)
                {
                    float breathe = 0.82f + Mathf.Sin(pullElapsed * 4f) * 0.05f;
                    Color bc = blackout.color;
                    blackout.color = new Color(bc.r, bc.g, bc.b, breathe);
                }

                Collider2D[] cols = Physics2D.OverlapCircleAll(center, pullRadius);
                foreach (Collider2D c in cols)
                {
                    Enemy en = c.GetComponentInParent<Enemy>();
                    if (en == null) continue;

                    int eid = en.GetInstanceID();

                    if (en.IsDead)
                    {
                        if (spagLines.TryGetValue(eid, out LineRenderer dl))
                        { if (dl != null) Destroy(dl.gameObject); spagLines.Remove(eid); }
                        continue;
                    }

                    float d = Vector2.Distance(en.transform.position, center);

                    if (d < 0.6f)
                    {
                        if (absorbPct > 0f)
                        {
                            int absorbAmt = absorbPct >= 1f
                                ? Mathf.RoundToInt(en.MaxHealth)
                                : Mathf.Max(1, Mathf.RoundToInt(en.MaxHealth * absorbPct * Time.deltaTime));
                            en.TakeDamage(absorbAmt);
                            if (absorbHeals && !en.IsDead) PlayerHealth.Instance?.Heal(2);
                        }
                        enemiesAbsorbed++;
                        EMPRingEffect.Spawn(center, 0.5f, 0.12f);
                        if (spagLines.TryGetValue(eid, out LineRenderer al))
                        { if (al != null) Destroy(al.gameObject); spagLines.Remove(eid); }
                        continue;
                    }

                    en.PullToward(center, pullForce * 2.8f);  // extra strong — cinematic pull

                    // Spaghettification line from enemy to singularity
                    if (!spagLines.ContainsKey(eid))
                    {
                        LineRenderer slr = MakeLR(0.05f, 0.01f, 935);
                        slr.startColor = new Color(0.8f, 0.35f, 1f, 0.7f);
                        slr.endColor   = new Color(0.35f, 0.1f, 1f, 0f);
                        spagLines[eid] = slr;
                    }
                    if (spagLines.TryGetValue(eid, out LineRenderer lr))
                    {
                        if (lr != null)
                        {
                            lr.SetPosition(0, en.transform.position);
                            lr.SetPosition(1, center);
                            float alpha = Mathf.Lerp(0.22f, 0.95f, 1f - d / pullRadius);
                            float width = Mathf.Lerp(0.05f, 0.015f, d / pullRadius); // thinner far away
                            lr.startColor = new Color(0.82f, 0.38f, 1f, alpha);
                            lr.endColor   = new Color(0.38f, 0.10f, 1f, 0f);
                            lr.startWidth = width;
                            lr.endWidth   = 0f;
                        }
                    }
                }

                // Remove stale lines (enemies destroyed outside the loop)
                var toRemove = new List<int>();
                foreach (var kvp in spagLines) if (kvp.Value == null) toRemove.Add(kvp.Key);
                foreach (int key in toRemove) spagLines.Remove(key);

                yield return null;
            }
        }
        finally
        {
            Time.timeScale = 1f;
            if (cam != null) cam.transform.position = actIIICamBase; // snap out of rumble
        }

        // Clean up all spaghettification lines
        foreach (var kvp in spagLines) if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        spagLines.Clear();

        // ─────────────────────────────────────────────────────────────────────
        //  ACT IV — THE LAST MOMENT (0.4s real freeze)
        //  Time stops. The void implodes. The world holds its breath.
        // ─────────────────────────────────────────────────────────────────────

        // Camera snaps TOWARD the singularity — zoom in for the impact frame
        Vector3 camPreCollapse = cam != null ? cam.transform.position : camCtrPos;
        float   sizePreCollapse = cam != null ? cam.orthographicSize : camWideSize;
        float   camZoomInSize   = sizePreCollapse * 0.62f;

        float zi = 0f;
        while (zi < 0.25f)
        {
            zi += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, zi / 0.25f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camPreCollapse, camCtrPos, t);
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(sizePreCollapse, camZoomInSize, t);
            }
            // Blackout pulses to near-solid as the singularity consumes everything
            if (blackout != null) blackout.color = new Color(0.01f, 0f, 0.04f, Mathf.Lerp(0.82f, 0.96f, t));
            yield return null;
        }

        // HARD FREEZE — the infinite density moment. The universe goes white.
        Time.timeScale = 0f;
        StartCoroutine(ImpactFlash(center, 0.10f, 0.18f));  // anime impact frame
        yield return new WaitForSecondsRealtime(0.28f);
        Time.timeScale = 1f;

        // ─────────────────────────────────────────────────────────────────────
        //  ACT V — THE DETONATION
        //  Reality snaps back. The singularity detonates with everything it ate.
        // ─────────────────────────────────────────────────────────────────────

        // Signal VFX coroutines to exit
        riftActive = false;
        yield return new WaitForSecondsRealtime(0.06f);

        // CollapseVFX handles all the explosions, rings, starburst, camera shake
        yield return StartCoroutine(CollapseVFX(center, enemiesAbsorbed, 1f));

        // Echo pulse 1 — the shockwave rebounds
        yield return new WaitForSecondsRealtime(0.15f);
        StartCoroutine(SpawnBloom(center, collapseRadius * 2.8f, new Color(0.55f, 0.15f, 1f, 0.55f), 0.25f));
        EMPRingEffect.Spawn(center, collapseRadius * 1.3f, 0.36f);
        EMPRingEffect.Spawn(center, collapseRadius * 2.2f, 0.56f);
        if (cam != null) StartCoroutine(ShakeCam(cam, 0.07f, 0.42f));

        // Echo pulse 2 — a distant tremor reaches the arena edge
        yield return new WaitForSecondsRealtime(0.30f);
        EMPRingEffect.Spawn(center, collapseRadius * 3.8f, 0.85f);
        StartCoroutine(SpawnBloom(center, collapseRadius * 4.5f, new Color(0.42f, 0.05f, 0.88f, 0.28f), 0.45f));
        StartCoroutine(ExpandingRing(center, collapseRadius * 4f, 0.55f, 0f, new Color(0.7f, 0.4f, 1f, 1f), 1f));

        // Echo pulse 3 — final silence
        yield return new WaitForSecondsRealtime(0.22f);
        EMPRingEffect.Spawn(center, collapseRadius * 5.5f, 1.2f);
        StartCoroutine(SpawnBloom(center, collapseRadius * 3.5f, new Color(0.35f, 0.02f, 0.75f, 0.18f), 0.60f));

        // ─────────────────────────────────────────────────────────────────────
        //  ACT VI — THE AFTERMATH (camera returns, world restores)
        // ─────────────────────────────────────────────────────────────────────

        Vector3 camPostCollapse = cam != null ? cam.transform.position : camOrigPos;
        float   sizePostCollapse = cam != null ? cam.orthographicSize : camOrigSize;

        e = 0f;
        while (e < 0.60f)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / 0.60f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camPostCollapse, camOrigPos, t);
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, camOrigPos.z);
                cam.orthographicSize   = Mathf.Lerp(sizePostCollapse, camOrigSize, t);
            }
            yield return null;
        }
        if (cam != null) { cam.transform.position = camOrigPos; cam.orthographicSize = camOrigSize; }

        StartCoroutine(FadeOutBlackout(blackout, 0.45f));

        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
        if (pixelCam != null) pixelCam.enabled = true;

        PlayerHealth.Instance?.SetInvincible(false);
        if (movement != null) movement.enabled = true;
        isCinematic = false;
    }

    // ─── PHASE 1: FORMATION ───────────────────────────────────────────────────

    private IEnumerator FormationVFX(Vector2 center, float power)
    {
        Time.timeScale = 0.25f;

        int crackCount = 5 + Mathf.RoundToInt(power * 7f);
        var cracks = new List<LineRenderer>(crackCount);
        for (int i = 0; i < crackCount; i++)
        {
            float ang = (i / (float)crackCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.3f, 0.3f);
            float len = pullRadius * UnityEngine.Random.Range(0.30f, 0.70f);
            Vector2 d   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 mid = center + d * len * 0.5f + (Vector2)(UnityEngine.Random.insideUnitCircle * 0.5f);
            Vector2 tip = center + d * len;

            LineRenderer lr = MakeLR(0.04f, 0f, 43);
            lr.positionCount = 3;
            lr.SetPosition(0, center); lr.SetPosition(1, mid); lr.SetPosition(2, tip);
            lr.startColor = new Color(0.85f, 0.7f, 1f, 0f);
            lr.endColor   = new Color(0.6f, 0.4f, 1f, 0f);
            cracks.Add(lr);
        }

        StartCoroutine(SpawnBloom(center, pullRadius * 0.35f, new Color(0.5f, 0.2f, 1f, 0.75f), 0.45f));

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float frac = t / 0.25f;
            foreach (var lr in cracks)
            {
                if (lr == null) continue;
                lr.startColor = new Color(0.85f, 0.7f, 1f, frac);
                lr.endColor   = new Color(0.6f, 0.4f, 1f, frac * 0.25f);
            }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.14f);
        Time.timeScale = 1f;
        StartCoroutine(FadeLinesOut(cracks, 0.30f));
        EMPRingEffect.Spawn(center, pullRadius * 0.55f, 0.22f);
        EMPRingEffect.Spawn(center, pullRadius,         0.38f);
    }

    // ─── PHASE 2a: VOID CORE ─────────────────────────────────────────────────

    private IEnumerator VoidCoreVFX(Vector2 center, float power)
    {
        GameObject voidGO = new GameObject("_SingVoid");
        SpriteRenderer voidSR = voidGO.AddComponent<SpriteRenderer>();
        voidSR.sprite = circleSprite;
        voidSR.sortingOrder = 38;
        Material voidMat = new Material(Shader.Find("Sprites/Default")
                        ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        voidSR.material = voidMat;
        voidGO.transform.position = new Vector3(center.x, center.y, 0f);

        GameObject ptGO = new GameObject("_SingPoint");
        SpriteRenderer ptSR = ptGO.AddComponent<SpriteRenderer>();
        ptSR.sprite = circleSprite;
        ptSR.sortingOrder = 42;
        if (additiveMat != null) ptSR.material = additiveMat;
        ptGO.transform.position = new Vector3(center.x, center.y, 0f);

        LineRenderer halo = MakeLR(0.11f, 0f, 39);
        halo.loop = true; halo.positionCount = 48;

        float elapsed = 0f;
        float targetR = pullRadius * (0.14f + power * 0.10f);

        while (riftActive)
        {
            elapsed += Time.deltaTime;
            float breathe = 1f + Mathf.Sin(elapsed * 2.2f) * 0.045f;
            float voidSize = Mathf.Lerp(0f, targetR * 2f,
                                Mathf.Sqrt(Mathf.Min(elapsed * 2.8f, 1f))) * breathe;
            voidGO.transform.localScale = Vector3.one * Mathf.Max(0f, voidSize);
            voidSR.color = new Color(0.01f, 0f, 0.03f, 0.97f);

            float ptSize = (0.22f + Mathf.Sin(elapsed * 8.5f) * 0.07f) * breathe;
            ptGO.transform.localScale = Vector3.one * ptSize;
            ptSR.color = new Color(1f, 0.92f, 1f, 0.95f);

            float hue = 0.76f + Mathf.Sin(elapsed * 0.65f) * 0.07f;
            Color haloCol = Color.HSVToRGB(hue, 0.8f, 1f);
            halo.startColor = halo.endColor = new Color(haloCol.r, haloCol.g, haloCol.b, 0.80f);
            float hr = voidSize * 0.5f;
            if (hr > 0.05f) SetRingPositions(halo, center, hr);
            yield return null;
        }

        float ct = 0f, startSz = voidGO.transform.localScale.x;
        while (ct < 0.28f)
        {
            ct += Time.unscaledDeltaTime;
            voidGO.transform.localScale = Vector3.one * Mathf.Lerp(startSz, 0f, ct / 0.28f);
            yield return null;
        }
        Destroy(voidMat); Destroy(voidGO); Destroy(ptGO); Destroy(halo.gameObject);
    }

    // ─── PHASE 2b: SPIRAL ARMS ───────────────────────────────────────────────

    private IEnumerator SpiralArmsVFX(Vector2 center, float power)
    {
        int   armCount = 2 + Mathf.RoundToInt(power * 2f);
        const int pts  = 26;
        float minR     = 0.4f;
        float maxR     = pullRadius * 0.88f;
        float windings = 1.15f;
        float rotSpd   = 0.52f;

        LineRenderer[] arms = new LineRenderer[armCount];
        for (int i = 0; i < armCount; i++) { arms[i] = MakeLR(0.09f, 0f, 36); arms[i].positionCount = pts; }

        float elapsed = 0f;
        while (riftActive)
        {
            elapsed += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / duration);
            float spiralIn = Mathf.Lerp(1f, 0.50f, t * t);
            float rotation = elapsed * rotSpd;
            float fade     = t < 0.14f ? t / 0.14f : t > 0.86f ? (1f - t) / 0.14f : 1f;

            for (int ai = 0; ai < armCount; ai++)
            {
                if (arms[ai] == null) continue;
                float baseAng = (ai / (float)armCount) * Mathf.PI * 2f + rotation;
                float hue = Mathf.Lerp(0.78f, 0.92f, (float)ai / Mathf.Max(1, armCount))
                           + Mathf.Sin(elapsed * 0.45f) * 0.05f;
                Color inner = Color.HSVToRGB(hue, 0.70f, 1.0f);
                Color outer = Color.HSVToRGB(hue + 0.1f, 0.90f, 0.7f);
                arms[ai].startColor = new Color(inner.r, inner.g, inner.b, 0.82f * fade);
                arms[ai].endColor   = new Color(outer.r, outer.g, outer.b, 0f);
                float w = (0.09f - t * 0.03f) * fade;
                arms[ai].startWidth = w; arms[ai].endWidth = 0f;

                for (int pi = 0; pi < pts; pi++)
                {
                    float param = pi / (float)(pts - 1);
                    float r     = Mathf.Lerp(minR, maxR * spiralIn, param);
                    float a     = baseAng + param * windings * Mathf.PI * 2f;
                    arms[ai].SetPosition(pi, new Vector3(
                        center.x + Mathf.Cos(a) * r,
                        center.y + Mathf.Sin(a) * r, 0f));
                }
            }
            yield return null;
        }

        float ft = 0f;
        while (ft < 0.14f)
        {
            ft += Time.unscaledDeltaTime;
            float f = 1f - ft / 0.14f;
            foreach (var arm in arms)
            {
                if (arm == null) continue;
                Color sc = arm.startColor; arm.startColor = new Color(sc.r, sc.g, sc.b, sc.a * f);
            }
            yield return null;
        }
        foreach (var arm in arms) if (arm != null) Destroy(arm.gameObject);
    }

    // ─── PHASE 2c: ACCRETION DISK ────────────────────────────────────────────

    private IEnumerator AccretionDiskVFX(Vector2 center, float power)
    {
        int count = 16 + Mathf.RoundToInt(power * 22f);
        var particles = new OrbParticle[count];

        for (int i = 0; i < count; i++)
        {
            float r   = UnityEngine.Random.Range(pullRadius * 0.26f, pullRadius * 0.82f);
            float spd = Mathf.Lerp(3.2f, 0.9f, r / pullRadius)
                      * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
            float hue = Mathf.Lerp(0.58f, 0.82f, r / pullRadius);
            Color col = Color.HSVToRGB(hue, 0.65f, 1f);

            LineRenderer lr = MakeLR(UnityEngine.Random.Range(0.03f, 0.07f), 0f, 37);
            lr.startColor = lr.endColor = col;

            particles[i] = new OrbParticle
            {
                lr = lr, angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                baseRadius = r, angSpeed = spd,
                tilt = UnityEngine.Random.Range(0.20f, 0.42f),
                twinkleSpd = UnityEngine.Random.Range(3f, 10f), col = col
            };
        }

        float elapsed = 0f;
        while (riftActive)
        {
            elapsed += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / duration);
            float spiralIn = Mathf.Lerp(1f, 0.32f, t * t * t);
            float fade     = (t < 0.12f ? t / 0.12f : 1f) * (t > 0.88f ? (1f - t) / 0.12f : 1f);

            foreach (var p in particles)
            {
                if (p.lr == null) continue;
                p.angle += p.angSpeed * Time.deltaTime;
                float r   = p.baseRadius * spiralIn;
                float tx1 = Mathf.Cos(p.angle) * r;          float ty1 = Mathf.Sin(p.angle) * r * p.tilt;
                float na  = p.angle + p.angSpeed * 0.07f;
                float tx2 = Mathf.Cos(na) * r;               float ty2 = Mathf.Sin(na) * r * p.tilt;

                p.lr.SetPosition(0, new Vector3(center.x + tx1, center.y + ty1, 0f));
                p.lr.SetPosition(1, new Vector3(center.x + tx2, center.y + ty2, 0f));

                float a = Mathf.Clamp01(fade * (0.7f + Mathf.Sin(elapsed * p.twinkleSpd) * 0.3f));
                p.lr.startColor = new Color(p.col.r, p.col.g, p.col.b, a);
                p.lr.endColor   = new Color(p.col.r, p.col.g, p.col.b, 0f);
            }
            yield return null;
        }
        foreach (var p in particles) if (p.lr != null) Destroy(p.lr.gameObject);
    }

    // ─── PHASE 2d: EVENT RINGS ───────────────────────────────────────────────

    private IEnumerator EventRingsVFX(Vector2 center, float power)
    {
        int ringCount = 2 + Mathf.RoundToInt(power * 3f);
        LineRenderer[] rings  = new LineRenderer[ringCount];
        float[]        phases = new float[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            rings[i] = MakeLR(0.05f, 0f, 35);
            rings[i].loop = true; rings[i].positionCount = 48;
            phases[i] = (i / (float)ringCount) * Mathf.PI * 2f;
        }

        float elapsed = 0f;
        while (riftActive)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / duration);
            float fade = t < 0.12f ? t / 0.12f : t > 0.88f ? (1f - t) / 0.12f : 1f;

            for (int i = 0; i < ringCount; i++)
            {
                if (rings[i] == null) continue;
                float frac   = (i + 1f) / (ringCount + 1f);
                float r      = Mathf.Lerp(pullRadius * 0.38f, pullRadius * 0.94f, frac)
                             * Mathf.Lerp(1f, 0.58f, t * t);
                float rotDir = i % 2 == 0 ? 1f : -1f;
                phases[i] += rotDir * (0.38f + frac * 0.55f) * Time.deltaTime;

                float hue   = Mathf.Lerp(0.75f, 0.94f, frac) + Mathf.Sin(elapsed * 0.75f + i) * 0.05f;
                Color col   = Color.HSVToRGB(hue, 0.80f, 1f);
                float pulse = 0.60f + Mathf.Sin(elapsed * 2.8f + i * 1.4f) * 0.22f;
                rings[i].startColor = rings[i].endColor =
                    new Color(col.r, col.g, col.b, pulse * fade * 0.68f);
                float w = 0.05f * (1f - t * 0.28f);
                rings[i].startWidth = rings[i].endWidth = w;

                for (int j = 0; j < 48; j++)
                {
                    float a  = (j / 48f) * Mathf.PI * 2f + phases[i];
                    float rx = r * (1f + Mathf.Sin(a * 3f + elapsed) * 0.04f);
                    float ry = r * (1f + Mathf.Cos(a * 2f + elapsed) * 0.04f);
                    rings[i].SetPosition(j, new Vector3(
                        center.x + Mathf.Cos(a) * rx,
                        center.y + Mathf.Sin(a) * ry, 0f));
                }
            }
            yield return null;
        }
        foreach (var r in rings) if (r != null) Destroy(r.gameObject);
    }

    // ─── PHASE 3: COLLAPSE ───────────────────────────────────────────────────

    private IEnumerator CollapseVFX(Vector2 center, int absorbed, float power)
    {
        Camera cam        = Camera.main;
        float  blastR         = scalingBlast ? Mathf.Min(collapseRadius + absorbed * 0.5f, 16f) : collapseRadius;
        float  collapseTotalPct = Mathf.Min(collapsePct + absorbed * collapsePerAbsorbPct, 2.0f); // cap at 200% max HP

        StartCoroutine(SpawnBloom(center, blastR * 2f, new Color(0.02f, 0f, 0.05f, 0.88f), 0.22f));
        if (isCinematic) StartCoroutine(SpawnBloom(center, blastR * 7f, new Color(1f, 1f, 1f, 1.0f), 0.20f));

        float freezeDur = isCinematic ? 0f : (starLevel >= 4 ? 0.18f : 0.08f); // cinematic handles its own freeze
        Time.timeScale  = isCinematic ? 1f : (starLevel >= 4 ? 0f : 0.03f);
        yield return new WaitForSecondsRealtime(freezeDur);
        Time.timeScale  = 1f;

        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, blastR))
        {
            Enemy en = c.GetComponentInParent<Enemy>();
            if (en == null || en.IsDead) continue;
            if (collapseTotalPct > 0f)
                en.TakeDamage(Mathf.RoundToInt(en.MaxHealth * collapseTotalPct));
            Vector2 d = ((Vector2)en.transform.position - center).normalized;
            if (d.sqrMagnitude < 0.01f) d = UnityEngine.Random.insideUnitCircle.normalized;
            en.ForceKnockback(d, 18f);
        }

        StartCoroutine(SpawnBloom(center, blastR * 3.8f, new Color(1f, 0.95f, 1f, 1.0f), 0.32f));

        if (starLevel >= 3)
        {
            float ab = 0.20f;
            StartCoroutine(SpawnBloom(center + new Vector2( ab, 0f), blastR * 2f, new Color(1f, 0.2f, 0.2f, 0.42f), 0.26f));
            StartCoroutine(SpawnBloom(center + new Vector2(-ab, 0f), blastR * 2f, new Color(0.2f, 0.5f, 1f, 0.42f), 0.26f));
        }

        int lineCount = 18 + Mathf.RoundToInt(power * 30f);
        StartCoroutine(StarburstLines(center, lineCount, blastR * 1.15f, power, 0.13f));

        int shockCount = 2 + Mathf.RoundToInt(power * 3f);
        for (int i = 0; i < shockCount; i++)
        {
            float delay    = i * (0.045f + power * 0.012f);
            float shockR   = blastR * (0.65f + i * 0.38f);
            Color shockCol = Color.HSVToRGB(Mathf.Lerp(0.78f, 0.95f, (float)i / shockCount), 0.75f, 1f);
            StartCoroutine(ExpandingRing(center, shockR, 0.38f + i * 0.14f, delay, shockCol, power));
        }

        EMPRingEffect.Spawn(center, blastR * 0.30f, 0.14f);
        EMPRingEffect.Spawn(center, blastR * 0.60f, 0.27f);
        EMPRingEffect.Spawn(center, blastR,         0.48f);
        EMPRingEffect.Spawn(center, blastR * 1.40f, 0.72f);
        if (starLevel >= 4) EMPRingEffect.Spawn(center, blastR * 1.95f, 1.0f);

        StartCoroutine(CollapseDebris(center, 8 + Mathf.RoundToInt(power * 18f), blastR, power));

        if (cam != null)
            StartCoroutine(ShakeCam(cam, 0.14f + power * 0.40f, 0.65f + power * 0.90f, rumble: true));

        yield return new WaitForSecondsRealtime(0.55f + power * 0.80f);
    }

    // ─── Starburst ───────────────────────────────────────────────────────────

    private IEnumerator StarburstLines(Vector2 center, int count, float maxLen, float power, float growDur)
    {
        Color coreCol = Color.Lerp(new Color(0.65f, 0.9f, 1f, 1f),  new Color(1f, 0.45f, 1f, 1f),  power);
        Color glowCol = Color.Lerp(new Color(0.15f, 0.4f, 1f, 0.28f), new Color(0.8f, 0f, 1f, 0.22f), power);

        var cores = new LineRenderer[count]; var glows = new LineRenderer[count];
        var dirs  = new Vector2[count];      var lens  = new float[count];

        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f;
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            lens[i] = maxLen * UnityEngine.Random.Range(0.44f, 1f);
            float cw = UnityEngine.Random.Range(0.024f, 0.044f + power * 0.036f);

            cores[i] = MakeLR(cw,      0f, 44); glows[i] = MakeLR(cw * 5f, 0f, 42);
            Vector3 from = new Vector3(center.x + dirs[i].x * 0.15f, center.y + dirs[i].y * 0.15f, 0f);
            cores[i].SetPosition(0, from); cores[i].SetPosition(1, from);
            glows[i].SetPosition(0, from); glows[i].SetPosition(1, from);
            cores[i].startColor = coreCol; cores[i].endColor = new Color(coreCol.r, coreCol.g, coreCol.b, 0f);
            glows[i].startColor = glowCol; glows[i].endColor = new Color(glowCol.r, glowCol.g, glowCol.b, 0f);
        }

        float t = 0f;
        while (t < growDur)
        {
            t += Time.unscaledDeltaTime;
            float frac = Mathf.SmoothStep(0f, 1f, t / growDur);
            for (int i = 0; i < count; i++)
            {
                if (cores[i] == null) continue;
                Vector3 tip = new Vector3(center.x + dirs[i].x * lens[i] * frac,
                                          center.y + dirs[i].y * lens[i] * frac, 0f);
                cores[i].SetPosition(1, tip); glows[i].SetPosition(1, tip);
            }
            yield return null;
        }

        float fadeDur = 0.40f + power * 0.55f;
        float[] ca = new float[count]; float[] ga = new float[count];
        for (int i = 0; i < count; i++) { if (cores[i] != null) { ca[i] = cores[i].startColor.a; ga[i] = glows[i].startColor.a; } }
        float ft = 0f;
        while (ft < fadeDur)
        {
            ft += Time.unscaledDeltaTime; float frac = ft / fadeDur;
            for (int i = 0; i < count; i++)
            {
                if (cores[i] == null) continue;
                Color cs = cores[i].startColor;
                cores[i].startColor = new Color(cs.r, cs.g, cs.b, Mathf.Lerp(ca[i], 0f, frac));
                cores[i].startWidth = Mathf.Lerp(cores[i].startWidth, 0f, frac * 0.35f);
                Color gs = glows[i].startColor;
                glows[i].startColor = new Color(gs.r, gs.g, gs.b, Mathf.Lerp(ga[i], 0f, frac));
                glows[i].startWidth = Mathf.Lerp(glows[i].startWidth, 0f, frac * 0.35f);
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
        {
            if (cores[i] != null) Destroy(cores[i].gameObject);
            if (glows[i] != null) Destroy(glows[i].gameObject);
        }
    }

    // ─── Expanding ring ──────────────────────────────────────────────────────

    private IEnumerator ExpandingRing(Vector2 center, float maxR, float dur, float delay, Color col, float power)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        LineRenderer core = MakeLR(0.07f + power * 0.04f, 0f, 43);
        LineRenderer glow = MakeLR(0.24f + power * 0.12f, 0f, 41);
        core.loop = glow.loop = true; core.positionCount = glow.positionCount = 48;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float frac = elapsed / dur;
            float r = Mathf.Lerp(0.05f, maxR, Mathf.Sqrt(frac));
            float a = Mathf.Lerp(1f, 0f, frac * frac);
            core.startColor = core.endColor = new Color(col.r, col.g, col.b, a);
            glow.startColor = glow.endColor = new Color(col.r, col.g, col.b, a * 0.28f);
            core.startWidth = core.endWidth = Mathf.Lerp(0.07f + power * 0.04f, 0f, frac);
            glow.startWidth = glow.endWidth = Mathf.Lerp(0.24f + power * 0.12f, 0f, frac);
            for (int i = 0; i < 48; i++)
            {
                float ang = (i / 48f) * Mathf.PI * 2f;
                Vector3 p = new Vector3(center.x + Mathf.Cos(ang) * r, center.y + Mathf.Sin(ang) * r, 0f);
                core.SetPosition(i, p); glow.SetPosition(i, p);
            }
            yield return null;
        }
        if (core != null) Destroy(core.gameObject);
        if (glow != null) Destroy(glow.gameObject);
    }

    // ─── Debris ──────────────────────────────────────────────────────────────

    private IEnumerator CollapseDebris(Vector2 center, int count, float maxDist, float power)
    {
        float[] ang = new float[count]; float[] spd = new float[count]; float[] spiral = new float[count];
        var lrs = new LineRenderer[count];
        Color col = Color.Lerp(new Color(0.6f, 0.85f, 1f, 1f), new Color(1f, 0.4f, 1f, 1f), power);

        for (int i = 0; i < count; i++)
        {
            ang[i]    = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            spd[i]    = UnityEngine.Random.Range(3.5f, 7f + power * 4f);
            spiral[i] = UnityEngine.Random.Range(-1.4f, 1.4f);
            lrs[i]    = MakeLR(UnityEngine.Random.Range(0.03f, 0.06f), 0f, 40);
            lrs[i].startColor = col; lrs[i].endColor = new Color(col.r, col.g, col.b, 0f);
        }
        float dur = 0.50f + power * 0.40f; float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; float frac = t / dur;
            for (int i = 0; i < count; i++)
            {
                if (lrs[i] == null) continue;
                float dist = spd[i] * t; float a = ang[i] + spiral[i] * t;
                float a2 = ang[i] + spiral[i] * t - 0.12f; float d2 = Mathf.Max(0f, dist - 0.28f);
                lrs[i].SetPosition(0, new Vector3(center.x + Mathf.Cos(a)  * dist, center.y + Mathf.Sin(a)  * dist, 0f));
                lrs[i].SetPosition(1, new Vector3(center.x + Mathf.Cos(a2) * d2,   center.y + Mathf.Sin(a2) * d2,   0f));
                lrs[i].startColor = new Color(col.r, col.g, col.b, Mathf.Lerp(1f, 0f, frac));
            }
            yield return null;
        }
        foreach (var lr in lrs) if (lr != null) Destroy(lr.gameObject);
    }

    // ─── Screen bloom ────────────────────────────────────────────────────────

    private IEnumerator SpawnBloom(Vector2 center, float radius, Color col, float dur)
    {
        EnsureCircleSprite();
        GameObject go = new GameObject("_SingBloom");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite; sr.sortingOrder = 945;
        Material mat;
        if (additiveMat != null) { mat = additiveMat; sr.material = mat; }
        else { mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")); sr.material = mat; }
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * radius * 2f;
        sr.color = col;

        float t = 0f;
        while (t < dur) { t += Time.unscaledDeltaTime; sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / dur)); yield return null; }
        if (mat != additiveMat) Destroy(mat);
        Destroy(go);
    }

    // ─── Camera shake ────────────────────────────────────────────────────────

    private IEnumerator ShakeCam(Camera cam, float magnitude, float dur, bool rumble = false)
    {
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - elapsed / dur; float mag = magnitude * decay * decay;
            float x = Mathf.Sin(elapsed * 43f) * mag; float y = Mathf.Cos(elapsed * 31f) * mag;
            if (rumble) { float rm = magnitude * 0.40f * decay; x += UnityEngine.Random.Range(-rm, rm); y += UnityEngine.Random.Range(-rm, rm); }
            cam.transform.position = basePos + new Vector3(x, y, 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    // ─── Cinematic helpers ────────────────────────────────────────────────────

    private SpriteRenderer CreateBlackoutQuad(Camera cam, Vector2 center)
    {
        GameObject go  = new GameObject("_SingBlackout");
        Texture2D  tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sr.color            = new Color(0f, 0f, 0f, 0f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 900;
        Vector3 pos = cam != null
            ? new Vector3(cam.transform.position.x, cam.transform.position.y, 0f)
            : new Vector3(center.x, center.y, 0f);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 1000f;
        return sr;
    }

    private IEnumerator FadeOutBlackout(SpriteRenderer sr, float dur)
    {
        if (sr == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            if (sr != null) sr.color = new Color(0f, 0f, 0.04f, Mathf.Lerp(1f, 0f, t / dur));
            yield return null;
        }
        if (sr != null && sr.gameObject != null) Destroy(sr.gameObject);
    }

    // ─── Shared helpers ──────────────────────────────────────────────────────

    private IEnumerator FadeLinesOut(List<LineRenderer> lines, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; float f = t / dur;
            foreach (var lr in lines)
            {
                if (lr == null) continue;
                Color sc = lr.startColor; lr.startColor = new Color(sc.r, sc.g, sc.b, Mathf.Lerp(sc.a, 0f, f));
            }
            yield return null;
        }
        foreach (var lr in lines) if (lr != null) Destroy(lr.gameObject);
    }

    private void SetRingPositions(LineRenderer lr, Vector2 center, float radius)
    {
        int cnt = lr.positionCount;
        for (int i = 0; i < cnt; i++)
        {
            float a = (i / (float)cnt) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f));
        }
    }

    private void EnsureAdditiveMat()
    {
        if (additiveMat != null) return;
        Shader sh = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        if (sh != null) additiveMat = new Material(sh);
    }

    private void EnsureCircleSprite()
    {
        if (circleSprite != null) return;
        const int size = 64;
        Texture2D tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
            float a = Mathf.Clamp01(1f - d / half);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private LineRenderer MakeLR(float startW, float endW, int order)
    {
        EnsureAdditiveMat();
        GameObject go = new GameObject("_SingVfx");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 2;
        lr.startWidth = startW; lr.endWidth = endW; lr.sortingOrder = order;
        if (additiveMat != null) lr.material = additiveMat;
        return lr;
    }

    // ─── Charging pulse (inward-contracting rings that increase in frequency) ─

    private IEnumerator ChargingPulseVFX(Vector2 center, float duration)
    {
        float elapsed      = 0f;
        float nextPulse    = 0.45f;
        float pulseInterval = 0.45f;

        while (riftActive && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = elapsed / duration;
            pulseInterval = Mathf.Lerp(0.45f, 0.10f, t);
            nextPulse -= Time.unscaledDeltaTime;

            if (nextPulse <= 0f)
            {
                nextPulse = pulseInterval;
                float ringR = pullRadius * Mathf.Lerp(0.58f, 0.20f, t);
                float hue   = Mathf.Lerp(0.80f, 0.60f, t);
                Color rc    = Color.HSVToRGB(hue, 0.90f, 1f);
                StartCoroutine(InwardRing(center, ringR, Mathf.Lerp(0.36f, 0.16f, t), rc));
            }
            yield return null;
        }
    }

    private IEnumerator InwardRing(Vector2 center, float startR, float dur, Color col)
    {
        LineRenderer core = MakeLR(0.055f, 0f, 944);
        LineRenderer glow = MakeLR(0.18f,  0f, 942);
        core.loop = glow.loop = true;
        core.positionCount = glow.positionCount = 48;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float frac = elapsed / dur;
            float r    = Mathf.Lerp(startR, 0.04f, Mathf.Pow(frac, 0.65f));
            float a    = Mathf.Lerp(1f, 0f, frac * frac * frac);
            core.startColor = core.endColor = new Color(col.r, col.g, col.b, a);
            glow.startColor = glow.endColor = new Color(col.r, col.g, col.b, a * 0.28f);
            float w = Mathf.Lerp(0.055f, 0.018f, frac);
            core.startWidth = core.endWidth = w;
            glow.startWidth = glow.endWidth = w * 3.2f;
            for (int i = 0; i < 48; i++)
            {
                float ang = (i / 48f) * Mathf.PI * 2f;
                Vector3 p = new Vector3(center.x + Mathf.Cos(ang) * r, center.y + Mathf.Sin(ang) * r, 0f);
                core.SetPosition(i, p); glow.SetPosition(i, p);
            }
            yield return null;
        }
        if (core != null) Destroy(core.gameObject);
        if (glow != null) Destroy(glow.gameObject);
    }

    // ─── Inward starfield (particles accelerating toward the singularity) ──────

    private IEnumerator InwardStarfieldVFX(Vector2 center, float outerRadius, float duration)
    {
        const int count   = 90;
        var stars = new LineRenderer[count];
        var pos   = new Vector2[count];
        var spd   = new float[count];

        for (int i = 0; i < count; i++)
        {
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r   = UnityEngine.Random.Range(outerRadius * 0.25f, outerRadius);
            pos[i]    = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            spd[i]    = UnityEngine.Random.Range(1.8f, 5.5f);
            LineRenderer lr = MakeLR(UnityEngine.Random.Range(0.018f, 0.042f), 0f, 936);
            float hue = UnityEngine.Random.Range(0.70f, 0.96f);
            Color col = Color.HSVToRGB(hue, 0.75f, 1f);
            lr.startColor = col; lr.endColor = new Color(col.r, col.g, col.b, 0f);
            stars[i] = lr;
        }

        float elapsed = 0f;
        while (riftActive && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < count; i++)
            {
                if (stars[i] == null) continue;
                Vector2 toC  = center - pos[i];
                float   dist = toC.magnitude;

                if (dist < 0.25f)
                {
                    float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    pos[i] = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang))
                             * UnityEngine.Random.Range(outerRadius * 0.7f, outerRadius);
                    spd[i] = UnityEngine.Random.Range(1.8f, 5.5f);
                    continue;
                }

                float accel = 1f + (1f - Mathf.Clamp01(dist / outerRadius)) * (3f + t * 5f);
                pos[i] += toC.normalized * spd[i] * accel * Time.deltaTime;

                Vector2 from = pos[i];
                Vector2 to   = pos[i] + toC.normalized * Mathf.Min(0.35f, dist * 0.25f);
                stars[i].SetPosition(0, new Vector3(from.x, from.y, 0f));
                stars[i].SetPosition(1, new Vector3(to.x,   to.y,   0f));
                float alpha = Mathf.Clamp01(0.88f - dist / outerRadius * 0.3f);
                Color sc = stars[i].startColor;
                stars[i].startColor = new Color(sc.r, sc.g, sc.b, alpha);
            }
            yield return null;
        }
        foreach (var s in stars) if (s != null) Destroy(s.gameObject);
    }

    // ─── Impact flash (full white screen — anime impact frame) ───────────────

    private IEnumerator ImpactFlash(Vector2 center, float holdDuration, float fadeDuration)
    {
        GameObject go  = new GameObject("_SingImpact");
        Texture2D  tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sr.sortingOrder = 901;
        sr.color        = new Color(1f, 1f, 1f, 1f);
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * 1000f;

        yield return new WaitForSecondsRealtime(holdDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / fadeDuration));
            yield return null;
        }
        Destroy(tex);
        if (go != null) Destroy(go);
    }

    // ─── OnUpgraded ──────────────────────────────────────────────────────────

    protected override void OnUpgraded()
    {
        if (definition == null) return;
        starLevel = definition.StarLevel;
        switch (starLevel)
        {
            case 0:
                pullRadius = 7f; pullForce = 6f; duration = 4.5f;
                absorbPct = 0f; collapsePct = 0f; collapsePerAbsorbPct = 0f; collapseRadius = 3.0f;
                hasOrbitalPhase = false; scalingBlast = false; eventHorizon = false; absorbHeals = false;
                break;
            case 1: pullForce = 8f;  absorbPct = 0f; collapsePct = 0f; collapsePerAbsorbPct = 0f; break;
            case 2: duration = 5.5f; pullRadius = 8.5f; absorbPct = 0f; collapsePct = 0f; collapsePerAbsorbPct = 0f; collapseRadius = 3.5f; break;
            case 3: hasOrbitalPhase = true; pullForce = 10f; absorbPct = 0f; collapsePct = 0f; collapsePerAbsorbPct = 0f; collapseRadius = 4.0f; break;
            case 4: scalingBlast = true; pullRadius = 10f; duration = 6f; pullForce = 12f; absorbPct = 1.5f; collapsePct = 0.28f; collapsePerAbsorbPct = 0.018f; collapseRadius = 4.5f; break;
            case 5: eventHorizon = true; absorbHeals = true; execThreshold = 0.4f; pullForce = 14f; absorbPct = 1.0f; collapsePct = 0.60f; collapsePerAbsorbPct = 0.040f; collapseRadius = 5.0f; break;
        }
    }
}
