using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemySpawnEntry
{
    public Enemy prefab;
    public EnemyProfile profileOverride;
    [Range(0f, 1f)] public float weight;
}

/// <summary>
/// Vampire-Survivors-style horde spawner.
/// Enemies stream in continuously from just outside the camera edges.
/// Difficulty (count, speed, health, EXP) scales with player level.
/// Bosses spawn at every multiple of <see cref="bossLevelInterval"/>.
/// </summary>
public sealed class HordeSpawner : MonoBehaviour
{
    public static HordeSpawner Instance { get; private set; }

    // ── Enemy pool ────────────────────────────────────────────────────────────
    [Header("Enemy Pool")]
    [SerializeField] private List<EnemySpawnEntry> enemyPool = new List<EnemySpawnEntry>();

    [Header("Boss")]
    [SerializeField] private EnemySpawnEntry bossEntry;
    [SerializeField] private int bossLevelInterval = 10;
    [SerializeField] private AbilityDropPickup abilityDropPickupPrefab;

    // ── Spawn rate ────────────────────────────────────────────────────────────
    [Header("Spawn Rate")]
    [Tooltip("Seconds between spawns at level 1. Slow to start — player is weak.")]
    [SerializeField] private float baseSpawnInterval = 3.5f;
    [Tooltip("Interval shrinks by this per level. Noticeable ramp by level 10.")]
    [SerializeField] private float intervalReductionPerLevel = 0.1f;
    [SerializeField] private float minSpawnInterval = 0.22f;

    // ── Live cap ──────────────────────────────────────────────────────────────
    [Header("Live Enemy Cap")]
    [SerializeField] private int baseMaxLive = 4;
    [SerializeField] private int maxLiveGrowthPerLevel = 2;
    [SerializeField] private int absoluteMaxLive = 100;

    // ── Stat scaling per level ────────────────────────────────────────────────
    [Header("Stat Scaling (per player level)")]
    [Tooltip("+N% health per level above 1.")]
    [SerializeField] private float healthScalingPerLevel = 0.14f;
    [Tooltip("+N% move speed per level above 1.")]
    [SerializeField] private float speedScalingPerLevel = 0.025f;
    [Tooltip("+N% exp per level above 1. Scales up so enemies are worth more as EXP requirements grow.")]
    [SerializeField] private float expScalingPerLevel = 0.35f;

    // ── Boss multipliers ──────────────────────────────────────────────────────
    [Header("Boss Multipliers (stacks with normal scaling)")]
    [SerializeField] private float bossHealthBase = 30f;
    [SerializeField] private float bossHealthPerBoss = 8f;   // grows each boss
    [SerializeField] private float bossSpeedBonus = 0.3f;
    [SerializeField] private float bossExpBase = 20f;
    [SerializeField] private float bossExpPerBoss = 5f;

    // ── Spawn zone ────────────────────────────────────────────────────────────
    [Header("Spawn Zone")]
    [Tooltip("Extra units beyond camera edge.")]
    [SerializeField] private float spawnMargin = 1.5f;
    [Tooltip("Follows Player tag if empty.")]
    [SerializeField] private Transform spawnCenter;

    // ── State ─────────────────────────────────────────────────────────────────
    public int LiveEnemyCount { get; private set; }
    public bool IsActive { get; private set; }

    public event Action<int> OnBossSpawned;   // passes boss number (1,2,3…)
    public event Action    OnBossKilled;

    private int playerLevel = 1;
    private int bossesSpawned = 0;
    private Camera mainCam;

    // ── Computed helpers ──────────────────────────────────────────────────────
    private float SpawnInterval =>
        Mathf.Max(minSpawnInterval, baseSpawnInterval - (playerLevel - 1) * intervalReductionPerLevel);

    private int MaxLive =>
        Mathf.Min(absoluteMaxLive, baseMaxLive + (playerLevel - 1) * maxLiveGrowthPerLevel);

    private float HealthMult  => 1f + (playerLevel - 1) * healthScalingPerLevel;
    private float SpeedMult   => 1f + (playerLevel - 1) * speedScalingPerLevel;
    private float ExpMult     => 1f + (playerLevel - 1) * expScalingPerLevel;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;

        if (spawnCenter == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) spawnCenter = p.transform;
        }
        if (spawnCenter == null) spawnCenter = transform;

        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;

        if (enemyPool.Count == 0)
        {
            Debug.LogWarning("[HordeSpawner] Enemy pool is empty — add entries in the Inspector.");
            return;
        }

        IsActive = true;
        StartCoroutine(SpawnLoop());
    }

    private void OnDestroy()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
    }

    // ── Level-up handler ──────────────────────────────────────────────────────

    private void HandleLevelUp(int newLevel)
    {
        playerLevel = newLevel;

        if (newLevel % bossLevelInterval == 0 && bossEntry.prefab != null)
            StartCoroutine(SpawnBossRoutine());
    }

    // ── Main spawn coroutine ──────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (IsActive)
        {
            if (LiveEnemyCount < MaxLive)
                SpawnNormalEnemy();

            yield return new WaitForSeconds(SpawnInterval);
        }
    }

    private IEnumerator SpawnBossRoutine()
    {
        // Brief delay so the level-up card clears first
        yield return new WaitForSeconds(3f);

        bossesSpawned++;
        OnBossSpawned?.Invoke(bossesSpawned);

        float bossHealth = (bossHealthBase + (bossesSpawned - 1) * bossHealthPerBoss) * HealthMult;
        float bossSpeed  = SpeedMult + bossSpeedBonus;
        float bossExp    = (bossExpBase  + (bossesSpawned - 1) * bossExpPerBoss)  * ExpMult;

        Enemy boss = SpawnAt(bossEntry.prefab, bossEntry.profileOverride,
                             bossHealth, bossSpeed, bossExp, isBoss: true);

        if (boss != null)
            boss.OnDeath += _ => HandleBossDeath(boss);
    }

    // ── Spawn helpers ─────────────────────────────────────────────────────────

    private void SpawnNormalEnemy()
    {
        EnemySpawnEntry entry = PickWeightedEntry();
        if (entry.prefab == null) return;

        SpawnAt(entry.prefab, entry.profileOverride,
                HealthMult, SpeedMult, ExpMult, isBoss: false);
    }

    private Enemy SpawnAt(Enemy prefab, EnemyProfile profileOverride,
                          float healthMult, float speedMult, float expMult,
                          bool isBoss)
    {
        Vector2 pos = GetScreenEdgePosition();
        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity);

        if (profileOverride != null)
            enemy.ApplyProfile(profileOverride);

        enemy.ScaleStats(healthMult, speedMult, expMult, isBoss);

        LiveEnemyCount++;
        enemy.OnDeath += _ => LiveEnemyCount--;
        return enemy;
    }

    private void HandleBossDeath(Enemy boss)
    {
        OnBossKilled?.Invoke();

        if (abilityDropPickupPrefab != null)
            Instantiate(abilityDropPickupPrefab, boss.transform.position, Quaternion.identity);
    }

    // ── Screen-edge spawn position ────────────────────────────────────────────

    private Vector2 GetScreenEdgePosition()
    {
        if (mainCam == null || !mainCam.orthographic)
            return (Vector2)spawnCenter.position + UnityEngine.Random.insideUnitCircle.normalized * 14f;

        float m  = spawnMargin;
        float hH = mainCam.orthographicSize + m;
        float hW = mainCam.orthographicSize * mainCam.aspect + m;
        Vector2 c = mainCam.transform.position;

        // Distribute evenly along the full perimeter
        float top    = hW * 2f;
        float right  = hH * 2f;
        float bottom = hW * 2f;
        float left   = hH * 2f;
        float perimeter = top + right + bottom + left;

        float t = UnityEngine.Random.Range(0f, perimeter);

        if (t < top)
            return c + new Vector2(-hW + t, hH);
        t -= top;
        if (t < right)
            return c + new Vector2(hW, hH - t);
        t -= right;
        if (t < bottom)
            return c + new Vector2(hW - t, -hH);
        t -= bottom;
        return c + new Vector2(-hW, -hH + t);
    }

    // ── Weighted random ───────────────────────────────────────────────────────

    private EnemySpawnEntry PickWeightedEntry()
    {
        float total = 0f;
        foreach (EnemySpawnEntry e in enemyPool) total += Mathf.Max(0.001f, e.weight);

        float roll = UnityEngine.Random.Range(0f, total);
        float cum  = 0f;
        foreach (EnemySpawnEntry e in enemyPool)
        {
            cum += Mathf.Max(0.001f, e.weight);
            if (roll <= cum) return e;
        }
        return enemyPool[enemyPool.Count - 1];
    }

    // ── Control ───────────────────────────────────────────────────────────────

    public void StopSpawning() => IsActive = false;

    public void ResumeSpawning()
    {
        if (!IsActive)
        {
            IsActive = true;
            StartCoroutine(SpawnLoop());
        }
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Camera cam = Camera.main;
        if (cam == null || spawnCenter == null) return;

        float hH = cam.orthographicSize + spawnMargin;
        float hW = cam.orthographicSize * cam.aspect + spawnMargin;
        Vector3 c = cam.transform.position;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawLine(c + new Vector3(-hW, hH), c + new Vector3(hW, hH));
        Gizmos.DrawLine(c + new Vector3(hW, hH), c + new Vector3(hW, -hH));
        Gizmos.DrawLine(c + new Vector3(hW, -hH), c + new Vector3(-hW, -hH));
        Gizmos.DrawLine(c + new Vector3(-hW, -hH), c + new Vector3(-hW, hH));
    }
}
