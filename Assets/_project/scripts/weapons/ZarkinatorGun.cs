using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Easter-egg gun for Mr. Zarkinator. Wave-6 locked.
/// Each star applies an OOP concept as a live gameplay mechanic:
///   ★1 Classification – bullets tag enemies; same-class neighbours share damage
///   ★2 Abstraction    – every 3rd trigger fires an invisible ghost bullet that
///                       "implements" (detonates) 0.45 s later for 3× damage
///   ★3 Encapsulation  – kills drop collectible data-capsule buffs
///   ★4 Inheritance    – accumulates +dmg/pierce/speed per kill, resets on reload
///   ★5 Polymorphism   – each magazine the gun extends a random derived form
///                       + one-time spectacular cinematic on first activation
/// </summary>
public sealed class ZarkinatorGun : CursorGun
{
    // ── OOP star flags ───────────────────────────────────────────────────────
    private bool hasClassification     = false;
    private bool hasAbstraction        = false;
    private bool hasEncapsulation      = false;
    private bool hasInheritance        = false;
    private bool hasPolymorphism       = false;
    private bool polymorphCutsceneDone = false;
    private bool isZarkinatorActive    = false; // false when a non-Zark profile is equipped

    // ── ★1 Classification ────────────────────────────────────────────────────
    private int  nextBulletClass = 0;                                  // 0=Alpha 1=Beta 2=Gamma
    private readonly Dictionary<int, int>         classified  = new(); // instanceID → class
    private readonly Dictionary<int, LineRenderer> classRings  = new(); // instanceID → ring renderer
    private readonly Dictionary<int, float>        classExpiry = new(); // instanceID → expiry
    private readonly Dictionary<string, LineRenderer> classConnections = new(); // "id1_id2" → line
    private static readonly Color[] ClassColors =
    {
        new Color(1f,    0.20f, 0.20f, 0.90f),  // Alpha – red
        new Color(0.20f, 0.50f, 1f,   0.90f),  // Beta  – blue
        new Color(0.10f, 1f,   0.35f, 0.90f),  // Gamma – green
    };
    private const float ClassDuration = 18f;
    private const float SplashRadius  = 2.8f;

    // ── ★2 Abstraction ───────────────────────────────────────────────────────
    private int abstractCounter = 0;  // every 3rd trigger = abstract shot

    // ── ★3 Encapsulation ─────────────────────────────────────────────────────
    private struct Capsule { public GameObject go; public int type; public float expiry; }
    private readonly List<Capsule> capsules = new();
    private int   capsuleCycle     = 0;
    private bool  damageBoosted    = false;
    private int   damageBoostCount = 0;
    private static readonly Color[] CapsuleColors =
    {
        new Color(1f,   0.30f, 0.30f, 1f), // 0 Damage – red
        new Color(0.30f,0.55f, 1f,   1f), // 1 Reload – blue
        new Color(0.20f,1f,   0.35f, 1f), // 2 Speed  – green
    };

    // ── ★4 Inheritance ───────────────────────────────────────────────────────
    private int   inheritStacks    = 0;
    private int   inheritDamage    = 0;
    private int   inheritPierce    = 0;
    private float inheritSpeedMult = 1f;
    private readonly List<GameObject> inheritGlows = new();

    // ── ★5 Polymorphism ──────────────────────────────────────────────────────
    private int polyForm = 0;  // 0=Pistol 1=Shotgun 2=Sniper 3=Chaos

    // ── ★5 Zark Token (enemy drop, re-triggers cutscene) ─────────────────────
    private bool       tokenLoaded  = false;
    private int        savedAmmo    = 0;
    private int        savedMagSize = 0;
    private GameObject tokenGo     = null;
    private static readonly Color[] FormColors =
    {
        new Color(1f, 0f, 0f),  // 0 Pistol  – red
        new Color(0f, 1f, 0f),  // 1 Shotgun – green
        new Color(0f, 0f, 1f),  // 2 Sniper  – blue
        new Color(1f, 1f, 1f),  // 3 Chaos   – white
    };

    private static readonly Color ZarkGreen = new Color(0f, 1f, 0.25f, 1f);

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void OnEnable()  { Enemy.AnyEnemyDied += OnAnyEnemyDied; }
    private void OnDisable() { Enemy.AnyEnemyDied -= OnAnyEnemyDied; }
    private void OnDestroy()
    {
        Enemy.AnyEnemyDied -= OnAnyEnemyDied;
        if (Time.timeScale < 0.5f) Time.timeScale = 1f;
    }

    // ── Shared enemy-death router ────────────────────────────────────────────
    private void OnAnyEnemyDied(Enemy enemy)
    {
        if (hasEncapsulation) HandleEncapsulationKill(enemy);
        if (hasInheritance)   HandleInheritanceKill(enemy);
        if (hasPolymorphism && tokenGo == null && !tokenLoaded && Random.value <= 0.01f)
            SpawnZarkToken(enemy.transform.position);
    }

    // ── Gun overrides ────────────────────────────────────────────────────────
    public override void ApplyProfile(GunProfile prof)
    {
        base.ApplyProfile(prof);
        ResetAll();
        isZarkinatorActive = prof != null && prof.DisplayName == "The Zarkinator";
    }

    public override void ApplyUpgrade(GunUpgrade upgrade)
    {
        base.ApplyUpgrade(upgrade);
        switch (upgrade.DisplayName)
        {
            case "Classification": hasClassification = true; isZarkinatorActive = true; break;
            case "Abstraction":    hasAbstraction    = true; isZarkinatorActive = true; break;
            case "Encapsulation":  hasEncapsulation  = true; isZarkinatorActive = true; break;
            case "Inheritance":    hasInheritance    = true; isZarkinatorActive = true; break;
            case "Polymorphism":
                hasPolymorphism = true; isZarkinatorActive = true;
                PickPolyForm();
                if (!polymorphCutsceneDone)
                {
                    polymorphCutsceneDone = true;
                    StartCoroutine(PolymorphismCutscene());
                }
                break;
        }
    }

    protected override IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // Compile flash every reload
        Vector3 root = transform.root.position;
        EMPRingEffect.Spawn(root, 2.0f, 0.32f);
        StartCoroutine(ZarkBloom(root, 3.5f, ZarkGreen, 0.45f));

        if (hasInheritance) ResetInheritance();
        if (hasPolymorphism) PickPolyForm();

        yield return new WaitForSeconds(reloadTime);
        ammoInMagazine = Mathf.Max(1, magazineSize);
        isReloading    = false;
        reloadRoutine  = null;
    }

    protected override void Update()
    {
        base.Update();
        TickClassRings();
        TickCapsules();
        TickInheritGlows();
    }

    // ── Fire ─────────────────────────────────────────────────────────────────
    protected override void Fire(Vector2 direction)
    {
        if (!isZarkinatorActive) { base.Fire(direction); return; }

        firedThisFrame = true;
        ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);
        nextFireTime   = Time.time + fireCooldown;
        ApplyRecoil(direction);
        InvokeOnShotFired();

        if (tokenLoaded)
        {
            tokenLoaded    = false;
            ammoInMagazine = savedAmmo;
            magazineSize   = savedMagSize;
            if (AmmoDisplay.Instance != null) AmmoDisplay.Instance.StopZarkRainbow();
            StartCoroutine(PolymorphismCutscene());
            return;
        }

        if (hasPolymorphism) { FirePolymorphic(direction); return; }

        bool isAbstractShot = hasAbstraction && (abstractCounter % 3 == 2);
        abstractCounter++;
        SpawnZarkBullet(direction, isAbstractShot);
    }

    private void SpawnZarkBullet(Vector2 dir, bool isAbstract)
    {
        if (projectilePrefab == null) return;

        LayerMask mask  = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
        float     spd   = (currentProjectileProfile?.Speed ?? projectileSpeed) * inheritSpeedMult;
        float     life  = currentProjectileProfile?.Lifetime ?? projectileLifetime;
        int       dmg   = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * GlobalDamageMultiplier));
        if (hasInheritance) dmg += inheritDamage;
        if (damageBoosted && damageBoostCount > 0)
        {
            dmg = Mathf.RoundToInt(dmg * 1.5f);
            damageBoostCount--;
            if (damageBoostCount <= 0) damageBoosted = false;
        }

        Projectile p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        p.Initialize(dir.normalized, spd, life, isAbstract ? 0 : dmg,
                     transform.root.gameObject, mask, false);
        if (currentProjectileProfile != null) p.ApplyProfile(currentProjectileProfile);
        p.transform.right = dir.normalized;

        if (gun5StarTrail) p.EnableTrail(ZarkGreen);
        if (hasInheritance && inheritPierce > 0) p.SetPiercing(inheritPierce);

        if (isAbstract)
        {
            // Ghost bullet stops on first enemy hit; AbstractImplement tracks position & explodes there
            p.SetTint(new Color(0.25f, 1f, 0.35f, 0.22f)); // SR gets ghost alpha; Light2D gets full-alpha green

            // Outline ring as child (stays attached until projectile dies)
            var outlineGo = new GameObject("_GhostOutline");
            outlineGo.transform.SetParent(p.transform, false);
            var olr = outlineGo.AddComponent<LineRenderer>();
            olr.useWorldSpace = false; olr.loop = true; olr.positionCount = 24;
            olr.startWidth = olr.endWidth = 0.055f;
            var osh = Shader.Find("Sprites/Default");
            if (osh != null) olr.material = new Material(osh);
            olr.startColor = olr.endColor = new Color(0.25f, 1f, 0.35f, 0.80f);
            for (int oi = 0; oi < 24; oi++)
            {
                float oa = oi * Mathf.PI * 2f / 24;
                olr.SetPosition(oi, new Vector3(Mathf.Cos(oa) * 0.22f, Mathf.Sin(oa) * 0.22f, 0f));
            }

            StartCoroutine(AbstractPulse(p));
            StartCoroutine(AbstractImplement(p, dmg * 3));
        }
        else if (hasClassification)
        {
            int cls = nextBulletClass;
            nextBulletClass = (nextBulletClass + 1) % 3;
            p.SetTint(ClassColors[cls]);
            p.OnProjectileImpact += col => OnClassBulletImpact(col, cls);
        }
    }

    // ── ★1 Classification ────────────────────────────────────────────────────
    private void OnClassBulletImpact(Collider2D col, int bulletClass)
    {
        Enemy hit = col.GetComponentInParent<Enemy>();
        if (hit == null || hit.IsDead) return;

        int id = hit.GetInstanceID();

        // Same-class: AoE splash to nearby classified neighbours
        if (classified.TryGetValue(id, out int existing) && existing == bulletClass)
        {
            int baseDmg = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * GlobalDamageMultiplier));
            if (hasInheritance) baseDmg += inheritDamage;

            Collider2D[] nearby = Physics2D.OverlapCircleAll(hit.transform.position, SplashRadius);
            bool anySplashed = false;
            foreach (Collider2D c in nearby)
            {
                Enemy other = c.GetComponentInParent<Enemy>();
                if (other == null || other.IsDead || other == hit) continue; // use reference compare, not collider
                if (classified.TryGetValue(other.GetInstanceID(), out int oc) && oc == bulletClass)
                {
                    other.TakeDamage(Mathf.RoundToInt(baseDmg * 0.9f));
                    StartCoroutine(ZarkBloom(other.transform.position, 1.4f, ClassColors[bulletClass], 0.28f));
                    anySplashed = true;
                }
            }
            // Pulse on the origin enemy too so the player sees it fire
            if (anySplashed) StartCoroutine(ZarkBloom(hit.transform.position, 2.0f, ClassColors[bulletClass], 0.32f));
        }

        classified[id]  = bulletClass;
        classExpiry[id] = Time.time + ClassDuration;
        EnsureClassRing(hit, id, bulletClass);
    }

    private void EnsureClassRing(Enemy enemy, int id, int cls)
    {
        if (classRings.TryGetValue(id, out LineRenderer existing) && existing != null)
        {
            // Update color if class changed
            existing.startColor = existing.endColor = ClassColors[cls];
            return;
        }
        var go = new GameObject("_ZarkClassRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true; lr.useWorldSpace = true; lr.positionCount = 32;
        lr.startWidth = lr.endWidth = 0.06f;
        var sh = Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);
        lr.startColor = lr.endColor = ClassColors[cls];
        classRings[id] = lr;
    }

    private void TickClassRings()
    {
        float now = Time.time;
        var expired = new List<int>();
        foreach (var kv in classRings)
        {
            int id = kv.Key;
            if (classExpiry.TryGetValue(id, out float ex) && now > ex)
                { if (kv.Value != null) Destroy(kv.Value.gameObject); expired.Add(id); continue; }
            Enemy e = FindEnemyById(id);
            if (e == null || e.IsDead)
                { if (kv.Value != null) Destroy(kv.Value.gameObject); expired.Add(id); continue; }

            Vector3 center = e.transform.position;
            const float r = 0.55f;
            var lr = kv.Value;
            if (lr == null) { expired.Add(id); continue; }
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
        }
        foreach (int id in expired)
        { classRings.Remove(id); classified.Remove(id); classExpiry.Remove(id); }

        UpdateClassConnections();
    }

    private void UpdateClassConnections()
    {
        // Group live classified enemies by class
        var buckets = new Dictionary<int, List<(int id, Vector3 pos)>>();
        foreach (var kv in classified)
        {
            int id = kv.Key; int cls = kv.Value;
            if (!classRings.ContainsKey(id)) continue;
            Enemy e = FindEnemyById(id);
            if (e == null || e.IsDead) continue;
            if (!buckets.ContainsKey(cls)) buckets[cls] = new List<(int, Vector3)>();
            buckets[cls].Add((id, e.transform.position));
        }

        // Create / update connection lines between same-class pairs
        var activeKeys = new System.Collections.Generic.HashSet<string>();
        foreach (var kvp in buckets)
        {
            int cls = kvp.Key; var bucket = kvp.Value;
            for (int a = 0; a < bucket.Count; a++)
            for (int b = a + 1; b < bucket.Count; b++)
            {
                int id1 = bucket[a].id, id2 = bucket[b].id;
                string key = id1 < id2 ? $"{id1}_{id2}" : $"{id2}_{id1}";
                activeKeys.Add(key);

                if (!classConnections.TryGetValue(key, out LineRenderer lr) || lr == null)
                {
                    var go = new GameObject("_ZarkConn");
                    lr = go.AddComponent<LineRenderer>();
                    lr.useWorldSpace = true; lr.positionCount = 2;
                    lr.startWidth = lr.endWidth = 0.045f;
                    var sh = Shader.Find("Sprites/Default");
                    if (sh != null) lr.material = new Material(sh);
                    classConnections[key] = lr;
                }
                Color col = ClassColors[cls];
                lr.startColor = lr.endColor = new Color(col.r, col.g, col.b, 0.55f);
                lr.SetPosition(0, bucket[a].pos);
                lr.SetPosition(1, bucket[b].pos);
            }
        }

        // Remove stale connections
        var toRemoveCon = new List<string>();
        foreach (var kv in classConnections)
        {
            if (!activeKeys.Contains(kv.Key))
            { if (kv.Value != null) Destroy(kv.Value.gameObject); toRemoveCon.Add(kv.Key); }
        }
        foreach (string k in toRemoveCon) classConnections.Remove(k);
    }

    private void ClearClassRings()
    {
        foreach (var kv in classRings)      if (kv.Value != null) Destroy(kv.Value.gameObject);
        foreach (var kv in classConnections) if (kv.Value != null) Destroy(kv.Value.gameObject);
        classRings.Clear(); classified.Clear(); classExpiry.Clear(); classConnections.Clear();
    }

    private static Enemy FindEnemyById(int id)
    {
        foreach (Enemy e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (e.GetInstanceID() == id) return e;
        return null;
    }

    // ── ★2 Abstraction ───────────────────────────────────────────────────────
    private IEnumerator AbstractPulse(Projectile p)
    {
        SpriteRenderer sr      = p != null ? p.GetComponent<SpriteRenderer>()          : null;
        Light2D        lt      = p != null ? p.GetComponentInChildren<Light2D>()        : null;
        LineRenderer   outline = p != null ? p.GetComponentInChildren<LineRenderer>()   : null;
        float t = 0f;
        while (p != null && sr != null)
        {
            t += Time.deltaTime * 7f;
            float pulse = (Mathf.Sin(t) + 1f) * 0.5f;
            sr.color = new Color(0.25f, 1f, 0.35f, Mathf.Lerp(0.06f, 0.32f, pulse));
            if (lt      != null) lt.intensity = Mathf.Lerp(0.3f, 1.5f, pulse);
            if (outline != null) outline.startColor = outline.endColor =
                new Color(0.25f, 1f, 0.35f, Mathf.Lerp(0.30f, 0.95f, pulse));
            yield return null;
        }
    }

    private IEnumerator AbstractImplement(Projectile p, int damage)
    {
        // Track position frame-by-frame — if bullet hits an enemy before 0.45s,
        // it gets destroyed by normal hit logic and we explode at lastPos.
        Vector2 lastPos = p != null ? (Vector2)p.transform.position : Vector2.zero;
        float elapsed = 0f;
        while (elapsed < 0.45f && p != null)
        {
            lastPos = (Vector2)p.transform.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector2 pos = p != null ? (Vector2)p.transform.position : lastPos;
        if (p != null) Destroy(p.gameObject);

        EMPRingEffect.Spawn(pos, 2.5f, 0.38f);
        StartCoroutine(ZarkBloom(pos, 5.5f, ZarkGreen, 0.50f));
        StartCoroutine(ZarkBloom(pos, 2.5f, Color.white, 0.22f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 2.5f);
        foreach (Collider2D c in hits)
        {
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead) e.TakeDamage(damage);
        }
    }

    // ── ★3 Encapsulation ─────────────────────────────────────────────────────
    private void HandleEncapsulationKill(Enemy enemy)
    {
        int type = capsuleCycle % 3;
        capsuleCycle++;
        SpawnCapsule(enemy.transform.position, type);
    }

    private void SpawnCapsule(Vector2 pos, int type)
    {
        const int sz = 24;
        var tex  = MakeCircleTex(sz);
        var go   = new GameObject("_ZarkCapsule");
        var sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        sr.color        = CapsuleColors[type];
        sr.sortingOrder = 45;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.55f;
        capsules.Add(new Capsule { go = go, type = type, expiry = Time.time + 10f });
        StartCoroutine(CapsuleBob(go));
    }

    private IEnumerator CapsuleBob(GameObject go)
    {
        if (go == null) yield break;
        Vector3 origin = go.transform.position;
        float t = 0f;
        while (go != null) { t += Time.deltaTime; go.transform.position = origin + Vector3.up * Mathf.Sin(t * 2.2f) * 0.18f; yield return null; }
    }

    private void TickCapsules()
    {
        if (!hasEncapsulation) return;
        float now = Time.time;
        Vector2 playerPos = transform.root.position;
        for (int i = capsules.Count - 1; i >= 0; i--)
        {
            var cap = capsules[i];
            bool expired = cap.go == null || now > cap.expiry;
            bool collected = !expired && Vector2.Distance(playerPos, cap.go.transform.position) < 1.0f;
            if (collected) { ApplyCapsule(cap.type); }
            if (expired || collected)
            {
                if (cap.go != null) Destroy(cap.go);
                capsules.RemoveAt(i);
            }
        }
    }

    private void ApplyCapsule(int type)
    {
        Vector3 pos = transform.root.position;
        StartCoroutine(ZarkBloom(pos, 3f, CapsuleColors[type], 0.55f));
        EMPRingEffect.Spawn(pos, 2.0f, 0.22f);
        switch (type)
        {
            case 0: damageBoosted = true; damageBoostCount = 6; break;
            case 1:
                if (isReloading && reloadRoutine != null) { StopCoroutine(reloadRoutine); reloadRoutine = null; }
                ammoInMagazine = magazineSize; isReloading = false;
                break;
            case 2:
                PlayerMovement pm = transform.root.GetComponentInChildren<PlayerMovement>();
                if (pm != null) pm.ApplySpeedBoost(1.35f, 6f);
                break;
        }
    }

    // ── ★4 Inheritance ───────────────────────────────────────────────────────
    private void HandleInheritanceKill(Enemy enemy)
    {
        if (inheritStacks >= 5) return;
        inheritStacks++;
        int roll = Random.Range(0, 3);
        if      (roll == 0) inheritDamage    += 3;
        else if (roll == 1) inheritPierce     = Mathf.Min(inheritPierce + 1, 4);
        else                inheritSpeedMult  = Mathf.Min(inheritSpeedMult + 0.15f, 2.0f);
        SpawnInheritGlow();
    }

    private void SpawnInheritGlow()
    {
        var go = new GameObject("_InheritGlow");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 48;
        const int sz = 16;
        var tex = MakeCircleTex(sz);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        sr.color  = new Color(0.15f, 1f, 0.4f, 0.80f);
        go.transform.localScale = Vector3.one * 0.4f;
        inheritGlows.Add(go);
    }

    private void TickInheritGlows()
    {
        if (inheritGlows.Count == 0) return;
        Vector3 center = transform.root.position;
        float t = Time.time * 1.8f;
        for (int i = 0; i < inheritGlows.Count; i++)
        {
            if (inheritGlows[i] == null) continue;
            float angle = t + i * (Mathf.PI * 2f / inheritGlows.Count);
            float r = 0.75f + 0.12f * Mathf.Sin(Time.time * 2.5f + i);
            inheritGlows[i].transform.position = center + new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
        }
    }

    private void ResetInheritance()
    {
        inheritStacks = 0; inheritDamage = 0; inheritPierce = 0; inheritSpeedMult = 1f;
        foreach (var g in inheritGlows) if (g != null) Destroy(g);
        inheritGlows.Clear();
    }

    // ── ★5 Polymorphism ──────────────────────────────────────────────────────

    // Ghost fire directions mirror FirePolymorphic exactly so clones match live behaviour.
    public override List<Vector2> GetGhostFireDirections(Vector2 aimDir)
    {
        if (!hasPolymorphism) return base.GetGhostFireDirections(aimDir);

        var dirs = new List<Vector2>();
        switch (polyForm)
        {
            case 0: // Pistol – 1 colossal forward shot
                dirs.Add(aimDir.normalized);
                break;
            case 1: // Shotgun – 8-pellet fan
            {
                float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                for (int i = 0; i < 8; i++)
                {
                    float a = (baseAngle - 37.5f + 10.7f * i) * Mathf.Deg2Rad;
                    dirs.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                }
                break;
            }
            case 2: // Sniper – 1 pierce-all beam forward
                dirs.Add(aimDir.normalized);
                break;
            case 3: // Chaos – 12 bullets in 360°
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2f / 12;
                    dirs.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                }
                break;
            }
        }
        return dirs.Count > 0 ? dirs : base.GetGhostFireDirections(aimDir);
    }

    private void PickPolyForm() { polyForm = Random.Range(0, 4); }

    // ── Zark Token spawn / collect ───────────────────────────────────────────
    private void SpawnZarkToken(Vector3 pos)
    {
        tokenGo = new GameObject("_ZarkToken");
        tokenGo.transform.position = pos;

        // Main body
        const int sz = 32;
        var tex = MakeCircleTex(sz);
        var sr  = tokenGo.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        sr.sortingOrder = 52;
        sr.color        = Color.white;
        tokenGo.transform.localScale = Vector3.one * 1.0f;

        // 4 orbiting satellites, one per form color
        var dotTransforms = new Transform[4];
        var dotSRs        = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            float initAng = i * Mathf.PI * 2f / 4;
            var dot = new GameObject("_TDot");
            dot.transform.SetParent(tokenGo.transform, false);
            var dtex = MakeCircleTex(16);
            var dsr  = dot.AddComponent<SpriteRenderer>();
            dsr.sprite       = Sprite.Create(dtex, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16);
            dsr.sortingOrder = 53;
            dsr.color        = FormColors[i];
            dot.transform.localScale    = Vector3.one * 0.38f;
            dot.transform.localPosition = new Vector3(Mathf.Cos(initAng) * 1.2f, Mathf.Sin(initAng) * 1.2f, 0f);
            dotTransforms[i] = dot.transform;
            dotSRs[i]        = dsr;
        }

        // Outer ring
        var ringGo = new GameObject("_TRing");
        ringGo.transform.SetParent(tokenGo.transform, false);
        var lr  = ringGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 32;
        lr.startWidth = lr.endWidth = 0.055f;
        var sh = Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);
        lr.startColor = lr.endColor = Color.white;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.45f, Mathf.Sin(a) * 1.45f, 0f));
        }

        StartCoroutine(ZarkTokenBob(tokenGo, sr, lr, dotTransforms, dotSRs));
    }

    private IEnumerator ZarkTokenBob(GameObject go, SpriteRenderer sr, LineRenderer lr,
                                     Transform[] dotT, SpriteRenderer[] dotSR)
    {
        if (go == null) yield break;
        Vector3 origin  = go.transform.position;
        float   t       = 0f;
        float   elapsed = 0f;
        const float lifetime = 14f;

        while (go != null)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                // Fade out and expire
                float fadeT = 0f;
                while (go != null && fadeT < 0.5f)
                {
                    fadeT += Time.deltaTime;
                    float a = Mathf.Lerp(1f, 0f, fadeT / 0.5f);
                    if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
                    if (lr != null) lr.startColor = lr.endColor = new Color(1f, 1f, 1f, a);
                    foreach (var dsr in dotSR) if (dsr != null) dsr.color = new Color(dsr.color.r, dsr.color.g, dsr.color.b, a);
                    yield return null;
                }
                Destroy(go);
                tokenGo = null;
                yield break;
            }

            t += Time.deltaTime;

            // Bob
            go.transform.position = origin + Vector3.up * Mathf.Sin(t * 3.0f) * 0.24f;

            // Rainbow cycle main body
            if (sr != null) sr.color = Color.HSVToRGB(t * 0.45f % 1f, 1f, 1f);

            // Orbit satellites
            for (int i = 0; i < dotT.Length; i++)
            {
                if (dotT[i] == null) continue;
                float ang = t * 1.8f + i * Mathf.PI * 2f / 4;
                dotT[i].localPosition = new Vector3(Mathf.Cos(ang) * 1.2f, Mathf.Sin(ang) * 1.2f, 0f);
                // Cycle dot colors through rainbow
                if (dotSR[i] != null)
                    dotSR[i].color = Color.HSVToRGB((t * 0.45f + i * 0.25f) % 1f, 1f, 1f);
            }

            // Ring pulse
            if (lr != null)
            {
                float pulse = (Mathf.Sin(t * 4f) + 1f) * 0.5f;
                Color rc = Color.HSVToRGB((t * 0.45f + 0.5f) % 1f, 0.8f, 1f);
                lr.startColor = lr.endColor = new Color(rc.r, rc.g, rc.b, Mathf.Lerp(0.35f, 1f, pulse));
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.04f, 0.09f, pulse);
            }

            // Pickup radius check
            if (Vector2.Distance(transform.root.position, go.transform.position) < 1.3f)
            {
                Vector3 collectPos = go.transform.position;
                Destroy(go);
                tokenGo = null;

                // Big collection fanfare
                for (int i = 0; i < 5; i++)
                    EMPRingEffect.Spawn(collectPos, 1.5f + i * 1.3f, 0.18f + i * 0.04f);
                StartCoroutine(ZarkBloom(collectPos, 11f, ZarkGreen, 0.90f));
                StartCoroutine(ZarkBloom(collectPos, 7f, new Color(1f, 0f, 1f, 1f), 0.65f));
                StartCoroutine(ZarkBloom(collectPos, 4.5f, new Color(1f, 1f, 0f, 1f), 0.50f));

                // Load token into gun
                savedAmmo    = ammoInMagazine;
                savedMagSize = magazineSize;
                ammoInMagazine = 1;
                magazineSize   = 1;
                tokenLoaded    = true;

                if (AmmoDisplay.Instance != null) AmmoDisplay.Instance.StartZarkRainbow();
                yield break;
            }

            yield return null;
        }

        tokenGo = null; // destroyed externally
    }

    private void FirePolymorphic(Vector2 direction)
    {
        if (projectilePrefab == null) return;
        LayerMask mask    = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
        float     spd     = currentProjectileProfile?.Speed ?? projectileSpeed;
        float     life    = currentProjectileProfile?.Lifetime ?? projectileLifetime;
        int       baseDmg = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * GlobalDamageMultiplier));
        if (hasInheritance) baseDmg += inheritDamage;

        switch (polyForm)
        {
            case 0: // extends Pistol – one colossal shot
            {
                var p = SpawnProjAt(firePoint.position, direction.normalized, spd * 1.1f, life, baseDmg * 3, mask);
                p.transform.right = direction.normalized;
                p.transform.localScale *= 2.8f;
                TintProj(p, FormColors[0]);
                StartCoroutine(ZarkBloom(firePoint.position, 4f, FormColors[0], 0.28f));
                break;
            }
            case 1: // extends Shotgun – 8 pellets wide fan
            {
                float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                for (int i = 0; i < 8; i++)
                {
                    float a = (baseAngle - 37.5f + 10.7f * i) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p = SpawnProjAt(firePoint.position, d, spd * 0.95f, life * 0.6f, Mathf.RoundToInt(baseDmg * 0.85f), mask);
                    p.transform.right = d;
                    TintProj(p, FormColors[1]);
                }
                break;
            }
            case 2: // extends Sniper – pierce all, 5× damage
            {
                var p = SpawnProjAt(firePoint.position, direction.normalized, spd * 4.5f, life * 1.8f, baseDmg * 5, mask);
                p.SetPiercing(99);
                p.transform.right = direction.normalized;
                p.transform.localScale = new Vector3(4.2f, 0.4f, 1f);
                TintProj(p, FormColors[2]);
                EMPRingEffect.Spawn(firePoint.position, 1.5f, 0.20f);
                break;
            }
            case 3: // extends Chaos – 12 bullets full 360°
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2f / 12;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p = SpawnProjAt(firePoint.position, d, spd * 0.9f, life * 0.8f, Mathf.RoundToInt(baseDmg * 0.75f), mask);
                    p.transform.right = d;
                    TintProj(p, FormColors[3]);
                }
                StartCoroutine(ZarkBloom(firePoint.position, 3.5f, FormColors[3], 0.30f));
                break;
            }
        }
    }

    // ── ★5 THE CINEMATIC ─────────────────────────────────────────────────────
    private IEnumerator PolymorphismCutscene()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        var     canvases   = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Vector3 origCamPos  = cam.transform.position;
        float   origCamSize = cam.orthographicSize;
        Vector3 playerPos   = transform.root.position;

        foreach (Canvas c in canvases) c.enabled = false;

        // ── DARK OVERLAY (builds drama; lives for the whole cutscene) ─────────
        var olTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int oy = 0; oy < 4; oy++) for (int ox = 0; ox < 4; ox++) olTex.SetPixel(ox, oy, Color.white);
        olTex.Apply();
        var olGo = new GameObject("_ZarkOverlay");
        var olSR = olGo.AddComponent<SpriteRenderer>();
        olSR.sprite = Sprite.Create(olTex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
        olSR.sortingOrder = 900;
        var olSh = Shader.Find("Sprites/Default");
        if (olSh != null) olSR.material = new Material(olSh);
        olGo.transform.position   = new Vector3(origCamPos.x, origCamPos.y, 0f);
        olGo.transform.localScale = Vector3.one * 1500f;
        olSR.color = new Color(0f, 0f, 0f, 0f);

        // ════════════════════════════════════════════════════════════════════
        // ACT I — IGNITION
        // ════════════════════════════════════════════════════════════════════
        for (int i = 0; i < 3; i++)
        {
            EMPRingEffect.Spawn(playerPos, 0.9f + i * 0.5f, 0.06f + i * 0.01f);
            yield return new WaitForSecondsRealtime(0.07f);
        }
        yield return new WaitForSecondsRealtime(0.14f);
        for (int i = 0; i < 3; i++)
        {
            EMPRingEffect.Spawn(playerPos, 1.8f + i * 1.0f, 0.12f + i * 0.02f);
            yield return new WaitForSecondsRealtime(0.10f);
        }
        EMPRingEffect.Spawn(playerPos, 5.5f, 0.26f);
        yield return new WaitForSecondsRealtime(0.14f);
        EMPRingEffect.Spawn(playerPos, 9f, 0.36f);

        StartCoroutine(ShakeCam(cam, 0.32f, 0.70f));
        StartCoroutine(ZarkBloom(playerPos, 11f, ZarkGreen, 0.85f));

        // Ease timescale down while dark overlay fades in
        float eT = 0f;
        while (eT < 0.28f)
        {
            eT += Time.unscaledDeltaTime;
            float ep = eT / 0.28f;
            Time.timeScale = Mathf.Lerp(1f, 0.07f, ep);
            if (olSR != null) olSR.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 0.72f, ep));
            yield return null;
        }
        Time.timeScale = 0.07f;
        if (olSR != null) olSR.color = new Color(0f, 0f, 0f, 0.72f);

        // Code rain — 70 strands falling through dark screen
        float camW = origCamSize * cam.aspect * 2.4f;
        float camH = origCamSize * 2.6f;
        var rain = new List<LineRenderer>();
        for (int i = 0; i < 70; i++)
        {
            var rGo = new GameObject("_ZR");
            var lr  = rGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2;
            float rw = Random.Range(0.018f, 0.052f);
            lr.startWidth = rw * 1.5f; lr.endWidth = rw * 0.3f;
            float rx = origCamPos.x + Random.Range(-camW * 0.5f, camW * 0.5f);
            float ry = origCamPos.y + Random.Range(-camH * 0.15f, camH * 0.55f);
            lr.SetPosition(0, new Vector3(rx, ry, 0f));
            lr.SetPosition(1, new Vector3(rx, ry - Random.Range(0.22f, 0.72f), 0f));
            var rsh = Shader.Find("Sprites/Default");
            if (rsh != null) lr.material = new Material(rsh);
            lr.startColor = lr.endColor = new Color(0f, Random.Range(0.75f, 1f), 0.25f, Random.Range(0.55f, 0.95f));
            rain.Add(lr);
        }
        float rainT = 0f, lastPls = 0f;
        const float rainDur = 2.8f;
        while (rainT < rainDur)
        {
            rainT += Time.unscaledDeltaTime;
            float tp = rainT / rainDur;
            float dy = Time.unscaledDeltaTime * 1.2f;
            foreach (var lr in rain)
            {
                if (lr == null) continue;
                Vector3 rp0 = lr.GetPosition(0), rp1 = lr.GetPosition(1);
                lr.SetPosition(0, rp0 + Vector3.down * dy);
                lr.SetPosition(1, rp1 + Vector3.down * dy);
                if (rp0.y < origCamPos.y - camH * 0.6f)
                {
                    float nx = origCamPos.x + Random.Range(-camW * 0.5f, camW * 0.5f);
                    float ny = origCamPos.y + camH * 0.55f;
                    lr.SetPosition(0, new Vector3(nx, ny, 0f));
                    lr.SetPosition(1, new Vector3(nx, ny - 0.55f, 0f));
                }
            }
            cam.orthographicSize = Mathf.Lerp(origCamSize, origCamSize * 2.0f, Mathf.SmoothStep(0f, 1f, tp));
            if (rainT - lastPls >= 0.48f) { EMPRingEffect.Spawn(playerPos, 2.5f + tp * 2.5f, 0.28f); lastPls = rainT; }
            yield return null;
        }
        foreach (var lr in rain) if (lr != null) Destroy(lr.gameObject);
        rain.Clear();

        // Transition stutter
        Time.timeScale = 0.14f;
        yield return new WaitForSecondsRealtime(0.22f);
        Time.timeScale = 1f;

        // ════════════════════════════════════════════════════════════════════
        // ACT II — FORMS ARISE
        // Each form materialises one at a time, 0.55 s staggered.
        // Dots orbit their ghost position; lightning arcs connect them all.
        // ════════════════════════════════════════════════════════════════════
        Vector2[]           ghostOff      = { Vector2.up * 3.5f, Vector2.right * 3.5f, Vector2.down * 3.5f, Vector2.left * 3.5f };
        var                 ghosts        = new List<GameObject>();
        var                 ghostSRArrays = new List<SpriteRenderer[]>();
        var                 ghostDotTrans = new List<Transform[]>();

        for (int fi = 0; fi < 4; fi++)
        {
            Vector3 gPos = playerPos + (Vector3)ghostOff[fi];
            StartCoroutine(ZarkBloom(gPos, 5f, FormColors[fi], 0.65f));
            EMPRingEffect.Spawn(gPos, 2.8f, 0.36f);
            StartCoroutine(ShakeCam(cam, 0.16f, 0.38f));

            var ghost = new GameObject("_PolyGhost");
            ghost.transform.position = gPos;

            // 12 orbital dots
            var dotT = new Transform[12];
            for (int di = 0; di < 12; di++)
            {
                float ang = di * Mathf.PI * 2f / 12;
                var dot = new GameObject("_D");
                dot.transform.SetParent(ghost.transform, false);
                dot.transform.localPosition = new Vector3(Mathf.Cos(ang) * 1.1f, Mathf.Sin(ang) * 1.1f, 0f);
                var dsr = dot.AddComponent<SpriteRenderer>();
                dsr.sortingOrder = 300 + fi;
                dsr.color  = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, 0f);
                dsr.sprite = Sprite.Create(MakeCircleTex(12), new Rect(0, 0, 12, 12), Vector2.one * 0.5f, 12);
                dotT[di] = dot.transform;
            }
            // Central glow
            var gGo = new GameObject("_G");
            gGo.transform.SetParent(ghost.transform, false);
            var gsr = gGo.AddComponent<SpriteRenderer>();
            gsr.sortingOrder = 302 + fi;
            gsr.color  = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, 0f);
            gsr.sprite = Sprite.Create(MakeCircleTex(24), new Rect(0, 0, 24, 24), Vector2.one * 0.5f, 24);
            gGo.transform.localScale = Vector3.one * 0.75f;

            ghosts.Add(ghost);
            ghostSRArrays.Add(ghost.GetComponentsInChildren<SpriteRenderer>());
            ghostDotTrans.Add(dotT);

            // Fade this ghost in while the next one queues
            float fadeIn = 0f;
            while (fadeIn < 0.42f)
            {
                fadeIn += Time.unscaledDeltaTime;
                float fa = Mathf.SmoothStep(0f, 0.90f, fadeIn / 0.42f);
                foreach (var sr in ghostSRArrays[fi])
                    sr.color = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, fa);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.13f);
        }

        // Lightning arcs: ring + diagonals between all ghost pairs
        var arcs = new List<LineRenderer>();
        int[][] arcPairs = { new[]{0,1}, new[]{1,2}, new[]{2,3}, new[]{3,0}, new[]{0,2}, new[]{1,3} };
        foreach (var pair in arcPairs)
        {
            var aGo = new GameObject("_ZArc");
            var alr = aGo.AddComponent<LineRenderer>();
            Vector3 pa = playerPos + (Vector3)ghostOff[pair[0]];
            Vector3 pb = playerPos + (Vector3)ghostOff[pair[1]];
            Vector3 mid = (pa + pb) * 0.5f + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            alr.useWorldSpace = true; alr.positionCount = 3;
            alr.startWidth = alr.endWidth = 0.055f;
            alr.SetPosition(0, pa); alr.SetPosition(1, mid); alr.SetPosition(2, pb);
            var ash = Shader.Find("Sprites/Default");
            if (ash != null) alr.material = new Material(ash);
            Color ac = Color.Lerp(FormColors[pair[0]], FormColors[pair[1]], 0.5f);
            alr.startColor = alr.endColor = new Color(ac.r, ac.g, ac.b, 0.80f);
            arcs.Add(alr);
        }

        // Hold: dots orbit, arcs pulse
        float holdT = 0f;
        while (holdT < 0.60f)
        {
            holdT += Time.unscaledDeltaTime;
            float apulse = (Mathf.Sin(holdT * 14f) + 1f) * 0.5f;
            foreach (var arc in arcs)
                if (arc != null) arc.startWidth = arc.endWidth = Mathf.Lerp(0.02f, 0.09f, apulse);
            for (int fi = 0; fi < 4; fi++)
            {
                if (ghosts[fi] == null) continue;
                var dots = ghostDotTrans[fi];
                for (int di = 0; di < dots.Length; di++)
                {
                    if (dots[di] == null) continue;
                    float ang = holdT * 1.7f + di * Mathf.PI * 2f / 12;
                    float r = 1.1f + 0.14f * Mathf.Sin(holdT * 5f + di * 0.42f);
                    dots[di].localPosition = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                }
                float gp = (Mathf.Sin(holdT * 9f + fi * 1.2f) + 1f) * 0.5f;
                foreach (var sr in ghostSRArrays[fi])
                    sr.color = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, Mathf.Lerp(0.65f, 0.96f, gp));
            }
            yield return null;
        }

        // ════════════════════════════════════════════════════════════════════
        // ACT III — TARGET LOCK
        // Time freezes, targeting lines snap to nearest enemies,
        // flash reveals the threat map.
        // ════════════════════════════════════════════════════════════════════
        Time.timeScale = 0f;

        Enemy[] targets = GetLiveEnemiesByDistance(playerPos);

        var tLines = new List<LineRenderer>();
        for (int fi = 0; fi < 4; fi++)
        {
            int ei = fi < targets.Length ? fi : (targets.Length > 0 ? 0 : -1);
            if (ei < 0 || targets[ei] == null) continue;
            var tlGo = new GameObject("_TL");
            var tlr  = tlGo.AddComponent<LineRenderer>();
            tlr.useWorldSpace = true; tlr.positionCount = 2;
            tlr.startWidth = 0.045f; tlr.endWidth = 0.012f;
            tlr.SetPosition(0, playerPos + (Vector3)ghostOff[fi]);
            tlr.SetPosition(1, targets[ei].transform.position);
            var tsh = Shader.Find("Sprites/Default");
            if (tsh != null) tlr.material = new Material(tsh);
            tlr.startColor = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, 0.95f);
            tlr.endColor   = new Color(FormColors[fi].r, FormColors[fi].g, FormColors[fi].b, 0.15f);
            tLines.Add(tlr);
        }

        StartCoroutine(ImpactFlash(playerPos, new Color(0.3f, 1f, 0.45f, 0.85f), 0.18f, 0.30f));
        yield return new WaitForSecondsRealtime(0.18f + 0.30f);

        foreach (var arc in arcs) if (arc != null) Destroy(arc.gameObject);
        arcs.Clear();

        // ════════════════════════════════════════════════════════════════════
        // ACT IV — ANNIHILATION
        // Each form fires toward the nearest enemies, then the player
        // unleashes a 36+18 bullet death blossom.
        // ════════════════════════════════════════════════════════════════════
        Time.timeScale = 1f;

        if (projectilePrefab != null)
        {
            LayerMask mask = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
            float spd  = (currentProjectileProfile?.Speed ?? projectileSpeed) * 1.25f;
            float life = (currentProjectileProfile?.Lifetime ?? projectileLifetime) * 1.6f;
            int   dmg  = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * GlobalDamageMultiplier));

            // ── Form 0 : Pistol — colossal piercing shot + 6 spreading mediums ──
            {
                Vector3 g0 = playerPos + (Vector3)ghostOff[0];
                Vector2 d0 = targets.Length > 0 && targets[0] != null
                    ? ((Vector2)targets[0].transform.position - (Vector2)g0).normalized
                    : ghostOff[0].normalized;
                var big = SpawnProjAt(g0, d0, spd * 1.3f, life, dmg * 5, mask);
                big.SetPiercing(4); big.transform.right = d0; big.transform.localScale *= 3.2f; TintProj(big, FormColors[0]);
                for (int i = 0; i < 6; i++)
                {
                    float sa = (i - 2.5f) * 16f * Mathf.Deg2Rad;
                    Vector2 sd = new Vector2(d0.x * Mathf.Cos(sa) - d0.y * Mathf.Sin(sa),
                                            d0.x * Mathf.Sin(sa) + d0.y * Mathf.Cos(sa)).normalized;
                    var p = SpawnProjAt(g0, sd, spd, life * 0.8f, dmg * 2, mask);
                    p.transform.right = sd; TintProj(p, FormColors[0]);
                }
                StartCoroutine(ZarkBloom(g0, 5f, FormColors[0], 0.50f));
                EMPRingEffect.Spawn(g0, 2.2f, 0.28f);
                if (ghosts.Count > 0 && ghosts[0] != null) StartCoroutine(GhostBurst(ghosts[0], ghostSRArrays[0], FormColors[0]));
            }
            yield return new WaitForSecondsRealtime(0.12f);

            // ── Form 1 : Shotgun — 22-pellet fan aimed at second enemy ────────
            {
                Vector3 g1 = playerPos + (Vector3)ghostOff[1];
                Vector2 d1 = targets.Length > 1 && targets[1] != null
                    ? ((Vector2)targets[1].transform.position - (Vector2)g1).normalized
                    : ghostOff[1].normalized;
                float ba = Mathf.Atan2(d1.y, d1.x) * Mathf.Rad2Deg;
                for (int i = 0; i < 22; i++)
                {
                    float a = (ba - 52.5f + 5f * i) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p = SpawnProjAt(g1, d, spd * 0.88f, life * 0.65f, Mathf.RoundToInt(dmg * 1.3f), mask);
                    p.transform.right = d; TintProj(p, FormColors[1]);
                }
                StartCoroutine(ZarkBloom(g1, 5f, FormColors[1], 0.50f));
                EMPRingEffect.Spawn(g1, 2.2f, 0.28f);
                if (ghosts.Count > 1 && ghosts[1] != null) StartCoroutine(GhostBurst(ghosts[1], ghostSRArrays[1], FormColors[1]));
            }
            yield return new WaitForSecondsRealtime(0.12f);

            // ── Form 2 : Sniper — 5 pierce-all beams each targeting an enemy ──
            {
                Vector3 g2 = playerPos + (Vector3)ghostOff[2];
                float fbAng = Mathf.Atan2(ghostOff[2].y, ghostOff[2].x) * Mathf.Rad2Deg;
                for (int i = 0; i < 5; i++)
                {
                    Vector2 sd;
                    if (i < targets.Length && targets[i] != null && !targets[i].IsDead)
                        sd = ((Vector2)targets[i].transform.position - (Vector2)g2).normalized;
                    else
                    {
                        float fa = (fbAng + (i - 2) * 30f) * Mathf.Deg2Rad;
                        sd = new Vector2(Mathf.Cos(fa), Mathf.Sin(fa));
                    }
                    var p = SpawnProjAt(g2, sd, spd * 5.5f, life * 2.2f, dmg * 7, mask);
                    p.SetPiercing(99); p.transform.right = sd; p.transform.localScale = new Vector3(5.0f, 0.32f, 1f);
                    TintProj(p, FormColors[2]);
                }
                StartCoroutine(ZarkBloom(g2, 5f, FormColors[2], 0.50f));
                EMPRingEffect.Spawn(g2, 2.2f, 0.28f);
                if (ghosts.Count > 2 && ghosts[2] != null) StartCoroutine(GhostBurst(ghosts[2], ghostSRArrays[2], FormColors[2]));
            }
            yield return new WaitForSecondsRealtime(0.12f);

            // ── Form 3 : Chaos — 24-bullet ring + 12 faster inner ring ────────
            {
                Vector3 g3 = playerPos + (Vector3)ghostOff[3];
                for (int i = 0; i < 24; i++)
                {
                    float a = i * Mathf.PI * 2f / 24;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p = SpawnProjAt(g3, d, spd * 0.95f, life * 0.95f, Mathf.RoundToInt(dmg * 1.5f), mask);
                    p.transform.right = d; TintProj(p, FormColors[3]);
                }
                for (int i = 0; i < 12; i++)
                {
                    float a = (i + 0.5f) * Mathf.PI * 2f / 12;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p = SpawnProjAt(g3, d, spd * 1.7f, life * 0.55f, Mathf.RoundToInt(dmg * 0.9f), mask);
                    p.transform.right = d; TintProj(p, new Color(1f, 0.55f, 1f));
                }
                StartCoroutine(ZarkBloom(g3, 5f, FormColors[3], 0.50f));
                EMPRingEffect.Spawn(g3, 2.2f, 0.28f);
                if (ghosts.Count > 3 && ghosts[3] != null) StartCoroutine(GhostBurst(ghosts[3], ghostSRArrays[3], FormColors[3]));
            }
            yield return new WaitForSecondsRealtime(0.20f);

            // ── Player: death blossom — 36 outer + 18 inner ───────────────────
            int   bDmg  = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * GlobalDamageMultiplier * 3.5f));
            float bSpd  = (currentProjectileProfile?.Speed ?? projectileSpeed) * 1.15f;
            float bLife = (currentProjectileProfile?.Lifetime ?? projectileLifetime) * 1.3f;
            for (int i = 0; i < 36; i++)
            {
                float a = i * Mathf.PI * 2f / 36;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var p = SpawnProjAt(playerPos, d, bSpd, bLife, bDmg, mask);
                p.transform.right = d; p.transform.localScale *= 1.9f; TintProj(p, FormColors[i % 4]);
            }
            for (int i = 0; i < 18; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / 18;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var p = SpawnProjAt(playerPos, d, bSpd * 1.55f, bLife * 0.65f, Mathf.RoundToInt(bDmg * 0.65f), mask);
                p.transform.right = d; TintProj(p, Color.white);
            }
        }

        foreach (var tl in tLines) if (tl != null) Destroy(tl.gameObject);
        tLines.Clear();

        // Climax: 6 cascading EMP rings + triple mega bloom + big shake
        for (int i = 0; i < 6; i++)
        {
            EMPRingEffect.Spawn(playerPos, 0.9f + i * 2.0f, 0.22f + i * 0.055f);
            yield return new WaitForSecondsRealtime(0.08f);
        }
        StartCoroutine(ZarkBloom(playerPos, 18f, ZarkGreen,               1.40f));
        StartCoroutine(ZarkBloom(playerPos, 12f, Color.white,             1.00f));
        StartCoroutine(ZarkBloom(playerPos,  7f, new Color(1f,0.5f,0f,1f), 0.80f));
        StartCoroutine(ShakeCam(cam, 0.42f, 1.1f));
        yield return new WaitForSecondsRealtime(0.95f);

        // ════════════════════════════════════════════════════════════════════
        // ACT V — ASCENSION
        // Ghosts already burst apart; overlay fades with camera return.
        // ════════════════════════════════════════════════════════════════════
        foreach (var g in ghosts) if (g != null) Destroy(g);
        ghosts.Clear();

        float retT       = 0f;
        float camFrom    = cam.orthographicSize;
        Vector3 camPosFrom = cam.transform.position;
        while (retT < 1.2f)
        {
            retT += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, retT / 1.2f);
            cam.orthographicSize   = Mathf.Lerp(camFrom, origCamSize, t);
            cam.transform.position = Vector3.Lerp(camPosFrom,
                new Vector3(origCamPos.x, origCamPos.y, cam.transform.position.z), t);
            if (olSR != null) olSR.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.72f, 0f, t));
            yield return null;
        }
        cam.orthographicSize   = origCamSize;
        cam.transform.position = new Vector3(origCamPos.x, origCamPos.y, cam.transform.position.z);
        Destroy(olTex); Destroy(olGo);

        EMPRingEffect.Spawn(playerPos, 8f, 0.32f);
        foreach (Canvas c in canvases) c.enabled = true;
    }

    private Enemy[] GetLiveEnemiesByDistance(Vector3 from)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var live = new List<Enemy>();
        foreach (Enemy e in all) if (e != null && !e.IsDead) live.Add(e);
        live.Sort((a, b) => Vector2.Distance(from, a.transform.position)
                                  .CompareTo(Vector2.Distance(from, b.transform.position)));
        return live.ToArray();
    }

    private IEnumerator GhostBurst(GameObject ghost, SpriteRenderer[] srs, Color col)
    {
        if (ghost == null) yield break;
        Vector3 center = ghost.transform.position;
        var startPos = new Vector3[srs.Length];
        for (int i = 0; i < srs.Length; i++)
            startPos[i] = srs[i] != null ? srs[i].transform.position : center;
        float t = 0f;
        while (t < 0.42f)
        {
            t += Time.unscaledDeltaTime;
            float frac = t / 0.42f;
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                Vector3 dir = (startPos[i] - center).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
                srs[i].transform.position = startPos[i] + dir * Mathf.SmoothStep(0f, 3.0f, frac);
                srs[i].color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.90f, 0f, frac));
            }
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
    }

    // ── VFX helpers ──────────────────────────────────────────────────────────
    private Projectile SpawnProjAt(Vector3 origin, Vector2 dir, float spd, float life, int dmg, LayerMask mask)
    {
        var p = Instantiate(projectilePrefab, origin, Quaternion.identity);
        p.Initialize(dir.normalized, spd, life, dmg, transform.root.gameObject, mask, false);
        if (currentProjectileProfile != null) p.ApplyProfile(currentProjectileProfile);
        return p;
    }

    private static void TintProj(Projectile p, Color col) => p.SetTint(col);

    private IEnumerator ZarkBloom(Vector2 center, float radius, Color col, float dur)
    {
        const int sz = 32;
        var tex = MakeCircleTex(sz);
        var go  = new GameObject("_ZarkBloom");
        var sr  = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        sr.sortingOrder = 940;
        var sh = Shader.Find("Particles/Additive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        if (sh != null) sr.material = new Material(sh);
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * radius * 2f;
        sr.color = col;

        float t = 0f;
        while (t < dur) { t += Time.unscaledDeltaTime; if (sr != null) sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / dur)); yield return null; }
        Destroy(tex); Destroy(go);
    }

    private IEnumerator ImpactFlash(Vector2 center, Color col, float hold, float fade)
    {
        const int sz = 4;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) tex.SetPixel(x, y, Color.white);
        tex.Apply();
        var go = new GameObject("_ZarkFlash");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
        sr.sortingOrder = 901;
        go.transform.position   = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = Vector3.one * 1200f;
        sr.color = col;

        yield return new WaitForSecondsRealtime(hold);
        float t = 0f;
        while (t < fade) { t += Time.unscaledDeltaTime; if (sr != null) sr.color = new Color(col.r, col.g, col.b, Mathf.Lerp(col.a, 0f, t / fade)); yield return null; }
        Destroy(tex); Destroy(go);
    }

    private IEnumerator ShakeCam(Camera cam, float mag, float dur)
    {
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float rm = mag * (1f - elapsed / dur);
            cam.transform.position = basePos + new Vector3(Random.Range(-rm, rm), Random.Range(-rm, rm), 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }

    private static Texture2D MakeCircleTex(int sz)
    {
        var tex  = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
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
        return tex;
    }

    // ── Reset ────────────────────────────────────────────────────────────────
    private void ResetAll()
    {
        hasClassification = hasAbstraction = hasEncapsulation = hasInheritance = hasPolymorphism = isZarkinatorActive = false;
        nextBulletClass = 0; abstractCounter = 0; capsuleCycle = 0;
        damageBoosted = false; damageBoostCount = 0;
        ClearClassRings();
        ResetInheritance();
        foreach (var c in capsules) if (c.go != null) Destroy(c.go);
        capsules.Clear();
        if (tokenGo != null) { Destroy(tokenGo); tokenGo = null; }
        tokenLoaded = false;
        if (AmmoDisplay.Instance != null) AmmoDisplay.Instance.StopZarkRainbow();
    }
}
