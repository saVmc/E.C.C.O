using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChupaSwordAbility : Ability
{
    [SerializeField] private GameObject auraZonePrefab;
    [SerializeField] private float      hitStopOffset  = 0.9f;
    [SerializeField] private int        swordSortOrder = 20;

    private const float SwordHandleOffset = 0f;
    private const bool  FlipSword         = false;

    private int   lungeDamage        = 8;
    private float lungeSpeed         = 20f;
    private float lungeRange         = 12f;
    private bool  piercesEnemies     = false;
    private bool  hasAfterimage      = false;
    private bool  chainsOnKill       = false;
    private bool  executionMode      = false;
    private float executionThreshold = 0.35f;
    private bool  hasCinematic       = false;
    private bool  hasCooldownReset   = false;

    private Rigidbody2D    rb;
    private SpriteRenderer playerSR;
    private PlayerMovement movement;
    private bool           isAnimating;

    private SpriteRenderer swordVisual;
    private GameObject     swordVisualGO;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        rb       = GetComponentInParent<Rigidbody2D>();
        playerSR = GetComponentInParent<SpriteRenderer>();
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void OnDisable()
    {
        if (!isAnimating) return;
        isAnimating = false;
        if (movement != null) movement.enabled = true;
        if (Time.timeScale < 0.5f) Time.timeScale = 1f;
        PlayerHealth.Instance?.SetInvincible(false);
    }

    private void EnsureSwordVisual()
    {
        if (swordVisualGO != null) return;
        if (definition == null) return;
        Sprite sprite = definition.VisualSprite;
        if (sprite == null) return;

        swordVisualGO = new GameObject("_ChupaSwordVisual");
        swordVisual   = swordVisualGO.AddComponent<SpriteRenderer>();
        swordVisual.sprite       = sprite;
        swordVisual.flipX        = FlipSword;
        swordVisual.sortingOrder = swordSortOrder;
        swordVisual.enabled      = false;

        float naturalLen = sprite.bounds.size.y;
        float desired    = definition.VisualSpriteWidth > 0f ? definition.VisualSpriteWidth : 2f;
        float scale      = naturalLen > 0.0001f ? desired / naturalLen : 1f;
        swordVisualGO.transform.localScale = Vector3.one * scale;
    }

protected override void Activate()
    {
        if (isAnimating) return;
        Enemy target = FindNearest();
        if (target == null) return;
        StartCoroutine(hasCinematic ? CinematicFinisher(target) : LungeAt(target, true));
    }

    private Enemy FindNearest()
    {
        Enemy[] all     = FindObjectsByType<Enemy>();
        Enemy   nearest = null;
        float   bestSq  = lungeRange * lungeRange;
        foreach (Enemy e in all)
        {
            if (e.IsDead) continue;
            float sq = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; nearest = e; }
        }
        return nearest;
    }

private IEnumerator LungeAt(Enemy target, bool allowChain)
    {
        isAnimating = true;
        if (movement != null) movement.enabled = false;
        rb.linearVelocity = Vector2.zero;
        PlayerHealth.Instance?.SetInvincible(true);

        Vector2 from      = rb.position;
        Vector2 targetPos = target.transform.position;
        Vector2 dir       = (targetPos - from).normalized;
        Vector2 to        = targetPos - dir * hitStopOffset;
        float   dur       = Mathf.Clamp(Vector2.Distance(from, to) / lungeSpeed, 0.05f, 0.18f);
        float   t         = 0f;

        ShowSword(dir);
        while (t < dur)
        {
            t += Time.deltaTime;
            float   ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 2f);
            Vector2 cur  = Vector2.Lerp(from, to, ease);
            rb.MovePosition(cur);
            UpdateSwordPosition(cur, dir);
            yield return null;
        }
        rb.MovePosition(to);
        rb.linearVelocity = Vector2.zero;
        UpdateSwordPosition(to, dir);
        HideSword();

        bool killed = DoHit(from, dir, target);

        Vector2 recoilPos  = to - dir * 1.8f;
        float   recoilDur  = 0.12f;
        float   recoilT    = 0f;
        while (recoilT < recoilDur)
        {
            recoilT += Time.deltaTime;
            float ease = Mathf.SmoothStep(0f, 1f, recoilT / recoilDur);
            rb.MovePosition(Vector2.Lerp(to, recoilPos, ease));
            yield return null;
        }
        rb.MovePosition(recoilPos);
        rb.linearVelocity = Vector2.zero;

        PlayerHealth.Instance?.SetInvincible(false);

        if (hasAfterimage) StartCoroutine(AfterimagePulses(targetPos));

        if (allowChain && chainsOnKill && killed)
        {
            Enemy next = FindNearest();
            if (next != null)
            {
                yield return new WaitForSeconds(0.06f);
                yield return LungeAt(next, false);
                yield break;
            }
        }

        if (movement != null) movement.enabled = true;
        isAnimating = false;
    }

private bool DoHit(Vector2 from, Vector2 dir, Enemy target)
    {
        if (target == null || target.IsDead) return false;
        bool killed = false;

        if (executionMode && target.HealthFraction <= executionThreshold)
        {
            target.ExecutionKill();
            MiniShockwave(target.transform.position, 3.5f, 18f);
            killed = true;
        }
        else
        {
            target.TakeDamage(lungeDamage);
            MiniShockwave(target.transform.position, 2f, 9f);
            killed = target.IsDead;
        }

        if (piercesEnemies)
        {
            Vector2 to   = (Vector2)target.transform.position - dir * hitStopOffset;
            float   dist = Vector2.Distance(from, to);
            foreach (RaycastHit2D h in Physics2D.CircleCastAll(from, 0.5f, dir, dist))
            {
                if (h.collider == null) continue;
                Enemy e = h.collider.GetComponentInParent<Enemy>();
                if (e != null && e != target && !e.IsDead)
                    e.TakeDamage(Mathf.CeilToInt(lungeDamage * 0.7f));
            }
        }

        if (killed) OnKill();
        return killed;
    }

    private void OnKill()
    {
        if (!hasCooldownReset && !hasCinematic) return;
        if (definition == null) return;
        if (hasCinematic)
        {
            float remaining = definition.Cooldown - (Time.time - lastUsedTime);
            lastUsedTime += remaining * 0.5f;
        }
        else
            lastUsedTime = -999f;
    }

    private IEnumerator AfterimagePulses(Vector2 pos)
    {
        const float dur = 2.2f, interval = 0.5f;
        float elapsed = 0f, next = interval;
        int   dmg = Mathf.CeilToInt(lungeDamage * 0.35f);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= next)
            {
                next += interval;
                foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, 2.8f))
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && !e.IsDead) e.TakeDamage(dmg);
                }
                EMPRingEffect.Spawn(pos, 2.8f, 0.4f);
            }
            yield return null;
        }
    }

    private void MiniShockwave(Vector2 center, float radius, float force)
    {
        EMPRingEffect.Spawn(center, radius, 0.5f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead)
                e.Knockback(((Vector2)e.transform.position - center).normalized, force);
        }
    }

    private void BigShockwave(Vector2 center, float speed, float radius)
    {
        EMPRingEffect.Spawn(center, radius,        1.0f);
        EMPRingEffect.Spawn(center, radius * 0.5f, 0.7f);
        foreach (Collider2D c in Physics2D.OverlapCircleAll(center, radius))
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null || e.IsDead) continue;
            Vector2 d = ((Vector2)e.transform.position - center).normalized;
            if (d.sqrMagnitude < 0.001f) d = Random.insideUnitCircle.normalized;
            e.ForceKnockback(d, speed);
        }
    }

private IEnumerator CinematicFinisher(Enemy target)
    {
        if (target == null || target.IsDead)
        {
            isAnimating = false;
            if (movement != null) movement.enabled = true;
            yield break;
        }

        isAnimating = true;
        if (movement != null) movement.enabled = false;
        rb.linearVelocity = Vector2.zero;
        PlayerHealth.Instance?.SetInvincible(true);

        // Track whether the target dies from an external source during the cinematic
        bool targetDied = false;
        System.Action<int> onTargetDeath = _ => targetDied = true;
        target.OnDeath += onTargetDeath;

        if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); shakeCoroutine = null; }

        Camera cam = Camera.main;
        PixelPerfectCamera pixelCam = cam != null ? cam.GetComponent<PixelPerfectCamera>() : null;
        if (pixelCam != null) pixelCam.enabled = false;

        SpriteRenderer targetSR = target != null ? target.GetComponent<SpriteRenderer>() : null;
        Rigidbody2D    targetRB = target != null ? target.GetComponent<Rigidbody2D>()    : null;
        RigidbodyType2D origBodyType = targetRB != null ? targetRB.bodyType : RigidbodyType2D.Dynamic;
        if (targetRB != null) { targetRB.bodyType = RigidbodyType2D.Static; targetRB.linearVelocity = Vector2.zero; }

        Canvas[] canvases     = FindObjectsByType<Canvas>();
        bool[]   canvasStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        { canvasStates[i] = canvases[i].enabled; canvases[i].enabled = false; }

        SpriteRenderer blackoutSR = CreateBlackoutQuad();

        int    pOrd = playerSR    != null ? playerSR.sortingOrder    : 0;
        string pLay = playerSR    != null ? playerSR.sortingLayerName : "Default";
        int    tOrd = targetSR    != null ? targetSR.sortingOrder    : 0;
        string tLay = targetSR    != null ? targetSR.sortingLayerName : "Default";
        int    sOrd = swordVisual != null ? swordVisual.sortingOrder : 0;
        string sLay = swordVisual != null ? swordVisual.sortingLayerName : "Default";

        if (playerSR    != null) { playerSR.sortingLayerName    = "Default"; playerSR.sortingOrder    = 950; }
        if (targetSR    != null) { targetSR.sortingLayerName    = "Default"; targetSR.sortingOrder    = 950; }
        if (swordVisual != null) { swordVisual.sortingLayerName = "Default"; swordVisual.sortingOrder = 960; }

        Vector3 camOrigPos  = cam != null ? cam.transform.position : Vector3.back * 10f;
        float   camOrigSize = cam != null ? cam.orthographicSize : 5f;
        Vector3 camHeldPos  = camOrigPos;

        Vector2 playerPos  = rb.position;
        Vector2 enemyPos   = target != null ? (Vector2)target.transform.position : playerPos;
        Vector2 midpoint   = (playerPos + enemyPos) * 0.5f;
        float   dist       = Vector2.Distance(playerPos, enemyPos);
        float   targetSize = Mathf.Clamp(dist * 0.55f, camOrigSize * 0.35f, camOrigSize * 0.9f);
        Vector3 camTarget  = new Vector3(midpoint.x, midpoint.y, camOrigPos.z);

        Vector2 dir     = (enemyPos - playerPos).normalized;
        Vector2 exitPos = enemyPos + dir * 3.0f;

        ShowSword(dir);

        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.4f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camOrigPos, camTarget, t);
                cam.orthographicSize   = Mathf.Lerp(camOrigSize, targetSize, t);
            }
            if (blackoutSR != null) blackoutSR.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }
        if (blackoutSR != null) blackoutSR.color = Color.black;
        if (cam != null) { cam.transform.position = camTarget; cam.orthographicSize = targetSize; }

        List<LineRenderer> speedLines = SpawnConvergentLines(enemyPos, 20, 3.5f, 8f, 930);
        yield return new WaitForSecondsRealtime(0.15f);
        StartCoroutine(AnimateConvergentLines(speedLines, enemyPos, 0.55f));
        yield return new WaitForSecondsRealtime(0.55f);

        yield return new WaitForSecondsRealtime(0.45f);

        rb.linearVelocity = Vector2.zero;
        Time.timeScale    = 0.08f;

        elapsed = 0f;
        const float dashDur = 0.35f;
        int trailSpawn = 0;

        List<LineRenderer> streaks = SpawnStreakLines(playerPos, dir, 6, 940);

        while (elapsed < dashDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(elapsed / dashDur);
            Vector2 cur = Vector2.Lerp(playerPos, exitPos, lerp);
            rb.position = cur;
            UpdateSwordPosition(cur, dir);
            UpdateStreakLines(streaks, playerPos, cur, dir);
            trailSpawn++;
            if (trailSpawn % 5 == 0 && playerSR != null)
                StartCoroutine(SpawnGhost(cur, playerSR, 0.6f));
            yield return null;
        }
        rb.position       = exitPos;
        rb.linearVelocity = Vector2.zero;
        HideSword();

        foreach (LineRenderer lr in speedLines) if (lr != null) Destroy(lr.gameObject);
        foreach (LineRenderer lr in streaks)    if (lr != null) Destroy(lr.gameObject);

        List<LineRenderer> slashMarks = new List<LineRenderer>();
        try
        {
            Time.timeScale = 0f;

            if (playerSR != null) playerSR.color = new Color(4f, 4f, 4f, 1f);
            if (targetSR != null) targetSR.color = new Color(4f, 4f, 4f, 1f);

            Vector2 slashFrom = playerPos - dir * 0.5f;
            Vector2 slashTo   = enemyPos  + dir * 1.5f;
            slashMarks = SpawnSlashMarks(slashFrom, slashTo, dir, 3, 0.18f, 950);

            EMPRingEffect.Spawn(enemyPos, 0.8f, 0.25f);
            EMPRingEffect.Spawn(enemyPos, 2.0f, 0.4f);
            EMPRingEffect.Spawn(enemyPos, 4.0f, 0.65f);

            yield return new WaitForSecondsRealtime(0.08f);

            if (playerSR != null) playerSR.color = Color.white;
            if (targetSR != null) targetSR.color = Color.white;
        }
        finally
        {
            Time.timeScale = 1f;
        }

        camHeldPos = cam != null ? cam.transform.position : camTarget;
        shakeCoroutine = StartCoroutine(ShakeCamera(cam, camHeldPos, 0.9f, 0.22f));
        StartCoroutine(FadeLineRenderers(slashMarks, 1.2f, true));

        yield return new WaitForSecondsRealtime(0.35f);
        EMPRingEffect.Spawn(enemyPos, 1.5f, 0.45f);
        yield return new WaitForSecondsRealtime(0.45f);
        EMPRingEffect.Spawn(enemyPos, 3.0f, 0.55f);
        yield return new WaitForSecondsRealtime(0.6f);

        shakeCoroutine = null;

        // Unsubscribe now that we're past the dangerous mid-cinematic zone
        if (target != null) target.OnDeath -= onTargetDeath;

        Vector2 deathPos = target != null ? (Vector2)target.transform.position : exitPos;

        StartCoroutine(FadeOutAndDestroyBlackout(blackoutSR, 0.5f));

        if (targetRB != null && targetRB.gameObject != null) targetRB.bodyType = origBodyType;

        bool shouldKill = !targetDied && target != null && !target.IsDead;
        if (shouldKill)
        {
            Vector3 baseScale = target.transform.localScale;
            EMPRingEffect.Spawn(deathPos, 1.2f, 0.3f);
            EMPRingEffect.Spawn(deathPos, 3.5f, 0.5f);
            EMPRingEffect.Spawn(deathPos, 7.0f, 0.8f);

            elapsed = 0f;
            while (elapsed < 0.35f && target != null && !target.IsDead)
            {
                elapsed += Time.deltaTime;
                float t     = elapsed / 0.35f;
                float scale = t < 0.55f
                    ? Mathf.Lerp(1f, 3.2f, t / 0.55f)
                    : Mathf.Lerp(3.2f, 0f, (t - 0.55f) / 0.45f);
                if (target != null && !target.IsDead) target.transform.localScale = baseScale * scale;
                float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                if (targetSR != null) targetSR.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            if (target != null && !target.IsDead) target.transform.localScale = baseScale;
            if (targetSR != null) targetSR.enabled = false;
            if (target != null && !target.IsDead) target.ExecutionKill();
            OnKill();
        }

        yield return new WaitForSeconds(0.12f);

        if (playerSR != null)
        { playerSR.color = Color.white; playerSR.sortingLayerName = pLay; playerSR.sortingOrder = pOrd; }
        if (targetSR != null)
        { targetSR.sortingLayerName = tLay; targetSR.sortingOrder = tOrd; }
        if (swordVisual != null)
        { swordVisual.sortingLayerName = sLay; swordVisual.sortingOrder = sOrd; }

        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.4f);
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(camHeldPos, camOrigPos, t);
                cam.orthographicSize   = Mathf.Lerp(targetSize, camOrigSize, t);
            }
            yield return null;
        }
        if (cam != null) { cam.transform.position = camOrigPos; cam.orthographicSize = camOrigSize; }

        if (pixelCam != null) pixelCam.enabled = true;

        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i] != null) canvases[i].enabled = canvasStates[i];

        BigShockwave(deathPos, 9f, 22f);

        if (auraZonePrefab != null)
        {
            GameObject zoneGO = Instantiate(auraZonePrefab, deathPos, Quaternion.identity);
            SwordAuraZone zone = zoneGO.GetComponent<SwordAuraZone>();
            if (zone != null) zone.Init(radius: 8f, slowMult: 0.55f, duration: 15f);
        }

        PlayerHealth.Instance?.SetInvincible(false);
        if (movement != null) movement.enabled = true;
        isAnimating = false;
    }

    private List<LineRenderer> SpawnConvergentLines(Vector2 center, int count,
                                                    float innerR, float outerR, int sortOrder)
    {
        var list = new List<LineRenderer>();
        for (int i = 0; i < count; i++)
        {
            float   angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
            Vector2 d     = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float   inner = Random.Range(innerR * 0.6f, innerR);
            float   outer = Random.Range(outerR * 0.8f, outerR);

            LineRenderer lr = CreateLineRenderer(Random.Range(0.03f, 0.07f), 0f, sortOrder);
            lr.SetPosition(0, center + d * inner);
            lr.SetPosition(1, center + d * outer);
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0f);
            list.Add(lr);
        }
        return list;
    }

    private IEnumerator AnimateConvergentLines(List<LineRenderer> lines, Vector2 center, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t     = elapsed / duration;
            float alpha = Mathf.Sin(t * Mathf.PI);
            float shrink = Mathf.Lerp(0f, 0.5f, t);

            foreach (LineRenderer lr in lines)
            {
                if (lr == null) continue;
                Vector3 p0 = lr.GetPosition(0);
                Vector3 p1 = lr.GetPosition(1);

                lr.SetPosition(0, Vector3.Lerp(p0, center, shrink * 0.3f));
                lr.startColor = lr.endColor = new Color(1f, 1f, 0.8f, alpha * 0.85f);
                _ = p1;
            }
            yield return null;
        }
    }

private List<LineRenderer> SpawnStreakLines(Vector2 from, Vector2 dir, int count, int sortOrder)
    {
        var list = new List<LineRenderer>();
        Vector2 perp = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < count; i++)
        {
            float   offset = (i - count * 0.5f) * 0.12f;
            Vector2 origin = from + perp * offset;

            LineRenderer lr = CreateLineRenderer(0.06f - i * 0.005f, 0f, sortOrder);
            lr.SetPosition(0, origin);
            lr.SetPosition(1, origin);
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.7f);
            list.Add(lr);
        }
        return list;
    }

    private void UpdateStreakLines(List<LineRenderer> streaks, Vector2 origin, Vector2 currentPos, Vector2 dir)
    {
        Vector2 perp = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < streaks.Count; i++)
        {
            LineRenderer lr = streaks[i];
            if (lr == null) continue;
            float   offset = (i - streaks.Count * 0.5f) * 0.12f;
            Vector2 tail   = origin + perp * offset - dir * (0.3f + i * 0.15f);
            lr.SetPosition(0, new Vector3(currentPos.x + perp.x * offset,
                                          currentPos.y + perp.y * offset, 0f));
            lr.SetPosition(1, new Vector3(tail.x, tail.y, 0f));
        }
    }

private List<LineRenderer> SpawnSlashMarks(Vector2 from, Vector2 to, Vector2 dir,
                                               int count, float spacing, int sortOrder)
    {
        var     list = new List<LineRenderer>();
        Vector2 perp = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < count; i++)
        {
            float   offset = (i - count * 0.5f) * spacing;
            Vector2 a      = from + perp * offset;
            Vector2 b      = to   + perp * offset;
            float   w      = i == 1 ? 0.08f : 0.04f;

            LineRenderer lr = CreateLineRenderer(w, w * 0.5f, sortOrder);
            lr.SetPosition(0, new Vector3(a.x, a.y, 0f));
            lr.SetPosition(1, new Vector3(b.x, b.y, 0f));
            lr.startColor = lr.endColor = Color.white;
            list.Add(lr);
        }
        return list;
    }

private IEnumerator SpawnGhost(Vector2 pos, SpriteRenderer source, float fadeDuration)
    {
        if (source == null || source.sprite == null) yield break;
        GameObject go    = new GameObject("_DashGhost");
        var        ghost = go.AddComponent<SpriteRenderer>();
        ghost.sprite           = source.sprite;
        ghost.flipX            = source.flipX;
        ghost.sortingLayerName = "Default";
        ghost.sortingOrder     = 945;
        ghost.color            = new Color(1f, 1f, 1f, 0.55f);
        go.transform.position   = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = source.transform.lossyScale;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (ghost != null) ghost.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 0f, elapsed / fadeDuration));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

private IEnumerator ShakeCamera(Camera cam, Vector3 basePos, float duration, float magnitude)
    {
        if (cam == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay  = 1f - (elapsed / duration);
            float mag    = magnitude * decay * decay;
            float shakeX = Mathf.Sin(elapsed * 38f) * mag;
            float shakeY = Mathf.Cos(elapsed * 29f) * mag;
            cam.transform.position = basePos + new Vector3(shakeX, shakeY, 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    private IEnumerator FadeLineRenderers(List<LineRenderer> lines, float duration, bool destroyAfter)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (LineRenderer lr in lines)
                if (lr != null) lr.startColor = lr.endColor = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        if (destroyAfter)
            foreach (LineRenderer lr in lines)
                if (lr != null) Destroy(lr.gameObject);
    }

    private IEnumerator FadeOutAndDestroyBlackout(SpriteRenderer sr, float duration)
    {
        if (sr == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (sr != null) sr.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        if (sr != null && sr.gameObject != null) Destroy(sr.gameObject);
    }

    private SpriteRenderer CreateBlackoutQuad()
    {
        GameObject go  = new GameObject("_CinematicBlackout");
        Texture2D  tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sr.color            = new Color(0f, 0f, 0f, 0f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 900;
        Camera cam = Camera.main;
        go.transform.position   = cam != null
            ? new Vector3(cam.transform.position.x, cam.transform.position.y, 0f)
            : new Vector3(rb.position.x, rb.position.y, 0f);
        go.transform.localScale = Vector3.one * 200f;
        return sr;
    }

private LineRenderer CreateLineRenderer(float startWidth, float endWidth, int sortOrder)
    {
        GameObject   go  = new GameObject("_VFXLine");
        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.startWidth        = startWidth;
        lr.endWidth          = endWidth;
        lr.useWorldSpace     = true;
        lr.sortingLayerName  = "Default";
        lr.sortingOrder      = sortOrder;
        lr.textureMode       = LineTextureMode.Stretch;

Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (sh != null)
        {
            lr.material       = new Material(sh);
            lr.material.color = Color.white;
        }
        return lr;
    }

private void ShowSword(Vector2 direction)
    {
        if (swordVisual == null) return;
        swordVisual.sortingOrder = swordSortOrder;
        swordVisual.flipX        = FlipSword;
        swordVisual.enabled      = true;
        UpdateSwordPosition(rb.position, direction);
    }

    private void UpdateSwordPosition(Vector2 playerPos, Vector2 direction)
    {
        if (swordVisual == null) return;
        swordVisual.transform.up = direction;
        float halfLen = swordVisual.sprite != null
            ? swordVisual.sprite.bounds.extents.y * swordVisualGO.transform.localScale.y
            : 0.5f;
        float dist = halfLen + SwordHandleOffset;
        swordVisual.transform.position = new Vector3(
            playerPos.x + direction.x * dist,
            playerPos.y + direction.y * dist, 0f);
    }

    private void HideSword() { if (swordVisual != null) swordVisual.enabled = false; }

protected override void OnUpgraded()
    {
        if (definition == null) return;
        if (definition.VfxPrefabA != null) auraZonePrefab = definition.VfxPrefabA;
        EnsureSwordVisual();

        switch (definition.StarLevel)
        {
            case 0:
                lungeDamage = 8; piercesEnemies = false; hasAfterimage = false;
                chainsOnKill = false; executionMode = false; hasCinematic = false;
                hasCooldownReset = false;
                break;
            case 1: lungeDamage = 13; piercesEnemies = true;  break;
            case 2: lungeDamage = 17; hasAfterimage  = true;  break;
            case 3: lungeDamage = 21; chainsOnKill   = true;  hasCooldownReset = true; break;
            case 4: lungeDamage = 26; executionMode  = true;  hasCooldownReset = true; break;
            case 5: lungeDamage = 34; hasCinematic   = true;  break;
        }
    }
}