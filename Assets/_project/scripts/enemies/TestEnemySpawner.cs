using UnityEngine;

public sealed class TestEnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxEnemies = 20;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float minSpawnRadius = 4f;

    [Header("Optional Profile Override")]
    [SerializeField] private EnemyProfile profileOverride;

    private float timer;
    private int liveCount;

    private void Update()
    {
        if (enemyPrefab == null || liveCount >= maxEnemies)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = spawnInterval;
        SpawnOne();
    }

    private void SpawnOne()
    {
        Vector2 offset = Random.insideUnitCircle.normalized
            * Random.Range(minSpawnRadius, spawnRadius);
        Vector3 spawnPos = transform.position + (Vector3)offset;

        Enemy enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        if (profileOverride != null)
            enemy.ApplyProfile(profileOverride);

        enemy.OnDeath += _ => liveCount--;
        liveCount++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minSpawnRadius);
    }
}
