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

public sealed class HordeSpawner : MonoBehaviour
{
    public static HordeSpawner Instance { get; private set; }

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

    [Header("Spawn Rate")]
    [SerializeField] private float baseSpawnInterval = 2.5f;
    [SerializeField] private float intervalReductionPerLevel = 0.1f;
    [SerializeField] private float intervalReductionPerWave  = 0.05f;
    [SerializeField] private float minSpawnInterval = 0.22f;

    [Header("Live Enemy Cap")]
    [SerializeField] private int baseMaxLive = 8;
    [SerializeField] private int maxLiveGrowthPerLevel = 2;
    [SerializeField] private int absoluteMaxLive = 100;

    [Header("Stat Scaling (per player level)")]
    [SerializeField] private float healthScalingPerLevel = 0.14f;
    [SerializeField] private float speedScalingPerLevel = 0.025f;
    [SerializeField] private float expScalingPerLevel = 0.08f;

    [Header("Boss Multipliers (stacks with normal scaling)")]
    [SerializeField] private float bossHealthBase = 30f;
    [SerializeField] private float bossHealthPerBoss = 8f;
    [SerializeField] private float bossSpeedBonus = 0.3f;
    [SerializeField] private float bossExpBase = 20f;
    [SerializeField] private float bossExpPerBoss = 5f;

    [Header("Spawn Zone")]
    [SerializeField] private float spawnMargin = 1.5f;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private BoxCollider2D spawnAreaCollider;
    [SerializeField] private Tilemap boundsTilemap;

    [Header("Out-of-Bounds Cleanup")]
    [SerializeField] private float oobCheckInterval = 2f;
    [SerializeField] private float oobKillMargin = 1f;

    [Header("Wave Settings")]
    [SerializeField] private int baseWaveSize = 10;
    [SerializeField] private int waveSizeGrowth = 4;
    [SerializeField] private float restBetweenWaves = 10f;
    [SerializeField] private int bossWaveInterval = 5;

    public int LiveEnemyCount { get; private set; }
    public int WaveEnemiesRemaining { get; private set; }
    public bool IsActive { get; private set; }
    public Enemy ActiveBoss { get; private set; }
    public int CurrentWave { get; private set; }
    public static int HighestWaveCompleted { get; private set; } = 0;

    public event Action<int>    OnBossSpawned;
    public event Action         OnBossKilled;
    public event Action<int>    OnWaveStarted;
    public event Action<int>    OnWaveCompleted;
    public event Action<string> OnWeaponUnlocked;

    private int playerLevel = 1;
    private int bossesSpawned = 0;
    private int waveSpawnedSoFar = 0;
    private Camera mainCam;
    private Bounds mapBounds;
    private int waveEnemiesRemaining = 0;

    private float SpawnInterval =>
        Mathf.Max(minSpawnInterval,
            baseSpawnInterval
            - (playerLevel  - 1) * intervalReductionPerLevel
            - (CurrentWave  - 1) * intervalReductionPerWave);

    private int MaxLive =>
        Mathf.Min(absoluteMaxLive, baseMaxLive + (playerLevel - 1) * maxLiveGrowthPerLevel);

    private float HealthMult  => (1f + (playerLevel - 1) * healthScalingPerLevel) * GameProgress.EnemyHealthMultiplier * PrestigeEffects.EnemyHealthScale;
    private float SpeedMult   => (1f + (playerLevel - 1) * speedScalingPerLevel)  * GameProgress.EnemySpeedMultiplier  * PrestigeEffects.EnemySpeedScale;
    private float ExpMult     => 1f + (playerLevel - 1) * expScalingPerLevel;

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

        if (spawnAreaCollider != null)
        {
            mapBounds = spawnAreaCollider.bounds;
            spawnAreaCollider.enabled = false;
        }
        else
        {
            if (boundsTilemap == null)
                boundsTilemap = FindAnyObjectByType<Tilemap>();
            if (boundsTilemap != null)
            {
                boundsTilemap.CompressBounds();
                mapBounds = boundsTilemap.localBounds;
                mapBounds.center += boundsTilemap.transform.position;
            }
            else
            {
                mapBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
            }
        }

        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;

        if (enemyPool.Count == 0)
        {
            Debug.LogWarning("[HordeSpawner] Enemy pool is empty â€” add entries in the Inspector.");
            return;
        }

        IsActive = true;
        StartCoroutine(SpawnLoop());
        StartCoroutine(OobCleanupLoop());
    }

    private void OnDestroy()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
    }

private void HandleLevelUp(int newLevel)
    {
        playerLevel = newLevel;
    }

private IEnumerator SpawnLoop()
    {
        yield return new WaitUntil(() => Time.timeScale > 0f);
        yield return new WaitForSeconds(2f);
        while (IsActive)
        {
            CurrentWave++;
            int waveSize = baseWaveSize + (CurrentWave - 1) * waveSizeGrowth;
            waveEnemiesRemaining = waveSize;
            WaveEnemiesRemaining = waveSize;
            waveSpawnedSoFar = 0;

            OnWaveStarted?.Invoke(CurrentWave);

if (CurrentWave % bossWaveInterval == 0)
                yield return StartCoroutine(SpawnBossRoutine());

int spawned = 0;
            while (spawned < waveSize)
            {
                if (LiveEnemyCount < MaxLive)
                {
                    SpawnNormalEnemy();
                    spawned++;
                    waveSpawnedSoFar = spawned;
                }
                yield return new WaitForSeconds(SpawnInterval);
            }

while (LiveEnemyCount > 0)
                yield return new WaitForSeconds(0.5f);

            OnWaveCompleted?.Invoke(CurrentWave);
            if (CurrentWave > HighestWaveCompleted)
            {
                HighestWaveCompleted = CurrentWave;
                CheckWeaponUnlocks(CurrentWave);
            }

yield return new WaitForSeconds(restBetweenWaves);
        }
    }

    private IEnumerator SpawnBossRoutine()
    {
        yield return new WaitForSeconds(3f);

        // Resolve boss prefab — fall back to the heaviest enemy in the pool
        Enemy bossPrefab = bossEntry.prefab;
        EnemyProfile bossProfileOverride = bossEntry.profileOverride;
        if (bossPrefab == null && enemyPool.Count > 0)
        {
            EnemySpawnEntry best = enemyPool[0];
            foreach (EnemySpawnEntry e in enemyPool)
                if (CurrentWave >= e.unlockAtWave && e.weight >= best.weight) best = e;
            bossPrefab = best.prefab;
            bossProfileOverride = best.profileOverride;
        }
        if (bossPrefab == null) yield break;

        bossesSpawned++;

        float bossHealth = (bossHealthBase + (bossesSpawned - 1) * bossHealthPerBoss) * HealthMult;
        float bossSpeed  = SpeedMult + bossSpeedBonus;
        float bossExp    = (bossExpBase  + (bossesSpawned - 1) * bossExpPerBoss)  * ExpMult;

        Enemy boss = SpawnAt(bossPrefab, bossProfileOverride,
                             bossHealth, bossSpeed, bossExp, isBoss: true);

        if (boss != null)
        {
            ActiveBoss = boss;
            OnBossSpawned?.Invoke(bossesSpawned);
            boss.OnDeath += _ => HandleBossDeath(boss);
        }
    }

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
        Vector2 pos = GetMapEdgePosition();
        Enemy enemy = Instantiate(prefab, pos, Quaternion.identity);

        if (profileOverride != null)
            enemy.ApplyProfile(profileOverride);

        enemy.ScaleStats(healthMult, speedMult, expMult, isBoss);

        LiveEnemyCount++;
        enemy.OnDeath += _ =>
        {
            LiveEnemyCount--;
            if (!isBoss) WaveEnemiesRemaining = Mathf.Max(0, WaveEnemiesRemaining - 1);
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

private IEnumerator OobCleanupLoop()
    {
        while (IsActive)
        {
            yield return new WaitForSeconds(oobCheckInterval);

            Bounds killBounds = mapBounds;
            killBounds.Expand(oobKillMargin * 2f);

            Enemy[] alive = FindObjectsByType<Enemy>();
            foreach (Enemy e in alive)
            {
                if (e == null || e.IsDead) continue;
                if (!killBounds.Contains(e.transform.position))
                {
                    LiveEnemyCount        = Mathf.Max(0, LiveEnemyCount - 1);
                    WaveEnemiesRemaining  = Mathf.Max(0, WaveEnemiesRemaining - 1);
                    Destroy(e.gameObject);
                }
            }
        }
    }

private Vector2 GetMapEdgePosition()
    {
        float minX = mapBounds.min.x + spawnMargin;
        float maxX = mapBounds.max.x - spawnMargin;
        float minY = mapBounds.min.y + spawnMargin;
        float maxY = mapBounds.max.y - spawnMargin;

        if (maxX <= minX || maxY <= minY)
            return (Vector2)spawnCenter.position + UnityEngine.Random.insideUnitCircle.normalized * 6f;

        float w = maxX - minX;
        float h = maxY - minY;
        float perimeter = 2f * (w + h);
        float t = UnityEngine.Random.Range(0f, perimeter);

        if (t < w)         return new Vector2(minX + t,      minY);
        t -= w;
        if (t < h)         return new Vector2(maxX,          minY + t);
        t -= h;
        if (t < w)         return new Vector2(maxX - t,      maxY);
        t -= w;
                           return new Vector2(minX,          maxY - t);
    }

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

        foreach (EnemySpawnEntry e in enemyPool)
            if (CurrentWave >= e.unlockAtWave) return e;
        return enemyPool[0];
    }

private static readonly (int wave, string name)[] WeaponUnlockTable = { (6, "THE ZARKINATOR") };

private void CheckWeaponUnlocks(int waveJustCompleted)
{
    foreach (var (wave, name) in WeaponUnlockTable)
        if (waveJustCompleted == wave)
            OnWeaponUnlocked?.Invoke(name);
}

public void StopSpawning() => IsActive = false;

    public void ResumeSpawning()
    {
        if (!IsActive)
        {
            IsActive = true;
            StartCoroutine(SpawnLoop());
        }
    }

private void OnDrawGizmosSelected()
    {
        Bounds b;
        if (spawnAreaCollider != null)
            b = spawnAreaCollider.bounds;
        else
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Vector3 min = b.min, max = b.max;
        Gizmos.DrawLine(new Vector3(min.x, min.y), new Vector3(max.x, min.y));
        Gizmos.DrawLine(new Vector3(max.x, min.y), new Vector3(max.x, max.y));
        Gizmos.DrawLine(new Vector3(max.x, max.y), new Vector3(min.x, max.y));
        Gizmos.DrawLine(new Vector3(min.x, max.y), new Vector3(min.x, min.y));
    }
}