using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public struct EnemySpawnEntry
{
    public Enemy prefab;
    public EnemyProfile profileOverride;
    [Range(0f, 1f)] public float weight;
    [Tooltip("This enemy type won't appear until this wave number.")]
    public int unlockAtWave;
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
    [SerializeField] private int bossLevelInterval = 5;
    [SerializeField] private AbilityDropPickup abilityDropPickupPrefab;

    [Header("Health Drop")]
    [SerializeField] private HealthPickup healthPickupPrefab;
    [Range(0f, 1f)]
    [SerializeField] private float healthDropChance = 0.05f;

    // ── Spawn rate ────────────────────────────────────────────────────────────
    [Header("Spawn Rate")]
    [Tooltip("Seconds between spawns at level 1. Slow to start — player is weak.")]
    [SerializeField] private float baseSpawnInterval = 2.5f;
    [Tooltip("Interval shrinks by this per level. Noticeable ramp by level 10.")]
    [SerializeField] private float intervalReductionPerLevel = 0.1f;
    [SerializeField] private float minSpawnInterval = 0.22f;

    // ── Live cap ──────────────────────────────────────────────────────────────
    [Header("Live Enemy Cap")]
    [SerializeField] private int baseMaxLive = 8;
    [SerializeField] private int maxLiveGrowthPerLevel = 2;
    [SerializeField] private int absoluteMaxLive = 100;

    // ── Stat scaling per level ────────────────────────────────────────────────
    [Header("Stat Scaling (per player level)")]
    [Tooltip("+N% health per level above 1.")]
    [SerializeField] private float healthScalingPerLevel = 0.14f;
    [Tooltip("+N% move speed per level above 1.")]
    [SerializeField] private float speedScalingPerLevel = 0.025f;
    [Tooltip("+N% exp per level above 1.")]
    [SerializeField] private float expScalingPerLevel = 0.08f;

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
    [Tooltip("Assign the wall/floor Tilemap so enemies can't spawn outside it.")]
    [SerializeField] private Tilemap boundsTilemap;

    // ── Wave settings ─────────────────────────────────────────────────────────
    [Header("Wave Settings")]
    [SerializeField] private int baseWaveSize = 10;
    [SerializeField] private int waveSizeGrowth = 4;
    [SerializeField] private float restBetweenWaves = 10f;
    [SerializeField] private int bossWaveInterval = 5;

    // ── State ─────────────────────────────────────────────────────────────────
    public int LiveEnemyCount { get; private set; }
    public bool IsActive { get; private set; }
    public Enemy ActiveBoss { get; private set; }
    public int CurrentWave { get; private set; }

    public event Action<int> OnBossSpawned;
    public event Action    OnBossKilled;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;

    private int playerLevel = 1;
    private int bossesSpawned = 0;
    private Camera mainCam;
    private Bounds mapBounds;
    private int waveEnemiesRemaining = 0;

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

        if (boundsTilemap == null)
            boundsTilemap = FindAnyObjectByType<Tilemap>();
        if (boundsTilemap != null)
        {
            boundsTilemap.CompressBounds();
            mapBounds = boundsTilemap.localBounds;
            // convert to world space
            mapBounds.center += boundsTilemap.transform.position;
        }
        else
        {
            mapBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }

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
    }

    // ── Main spawn coroutine ──────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        yield return new WaitUntil(() => Time.timeScale > 0f);
        yield return new WaitForSeconds(2f);
        while (IsActive)
        {
            CurrentWave++;
            int waveSize = baseWaveSize + (CurrentWave - 1) * waveSizeGrowth;
            waveEnemiesRemaining = waveSize;

            OnWaveStarted?.Invoke(CurrentWave);

            // Boss wave
            if (CurrentWave % bossWaveInterval == 0 && bossEntry.prefab != null)
                yield return StartCoroutine(SpawnBossRoutine());

            // Spawn all enemies in this wave
            int spawned = 0;
            while (spawned < waveSize)
            {
                if (LiveEnemyCount < MaxLive)
                {
                    SpawnNormalEnemy();
                    spawned++;
                }
                yield return new WaitForSeconds(SpawnInterval);
            }

            // Wait for all wave enemies to die
            while (LiveEnemyCount > 0)
                yield return new WaitForSeconds(0.5f);

            OnWaveCompleted?.Invoke(CurrentWave);

            // Rest between waves
            yield return new WaitForSeconds(restBetweenWaves);
        }
    }

    private IEnumerator SpawnBossRoutine()
    {
        // Brief delay so the level-up card clears first
        yield return new WaitForSeconds(3f);

        bossesSpawned++;

        float bossHealth = (bossHealthBase + (bossesSpawned - 1) * bossHealthPerBoss) * HealthMult;
        float bossSpeed  = SpeedMult + bossSpeedBonus;
        float bossExp    = (bossExpBase  + (bossesSpawned - 1) * bossExpPerBoss)  * ExpMult;

        Enemy boss = SpawnAt(bossEntry.prefab, bossEntry.profileOverride,
                             bossHealth, bossSpeed, bossExp, isBoss: true);

        if (boss != null)
        {
            ActiveBoss = boss;
            OnBossSpawned?.Invoke(bossesSpawned);
            boss.OnDeath += _ => HandleBossDeath(boss);
        }
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
        // Clamp inside map so enemies don't appear in void
        pos.x = Mathf.Clamp(pos.x, mapBounds.min.x + 1f, mapBounds.max.x - 1f);
        pos.y = Mathf.Clamp(pos.y, mapBounds.min.y + 1f, mapBounds.max.y - 1f);
        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity);

        if (profileOverride != null)
            enemy.ApplyProfile(profileOverride);

        enemy.ScaleStats(healthMult, speedMult, expMult, isBoss);

        LiveEnemyCount++;
        enemy.OnDeath += _ =>
        {
            LiveEnemyCount--;
            if (!isBoss && healthPickupPrefab != null && UnityEngine.Random.value < healthDropChance)
                Instantiate(healthPickupPrefab, enemy.transform.position, Quaternion.identity);
        };
        return enemy;
    }

    private void HandleBossDeath(Enemy boss)
    {
        ActiveBoss = null;
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
        foreach (EnemySpawnEntry e in enemyPool)
            if (CurrentWave >= e.unlockAtWave) total += Mathf.Max(0.001f, e.weight);

        float roll = UnityEngine.Random.Range(0f, total);
        float cum  = 0f;
        foreach (EnemySpawnEntry e in enemyPool)
        {
            if (CurrentWave < e.unlockAtWave) continue;
            cum += Mathf.Max(0.001f, e.weight);
            if (roll <= cum) return e;
        }
        // Fallback: first unlocked entry
        foreach (EnemySpawnEntry e in enemyPool)
            if (CurrentWave >= e.unlockAtWave) return e;
        return enemyPool[0];
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
