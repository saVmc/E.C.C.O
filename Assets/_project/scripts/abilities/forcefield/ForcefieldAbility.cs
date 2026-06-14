using System.Collections;
using UnityEngine;

public sealed class ForcefieldAbility : Ability
{
    [Header("Forcefield Settings")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private float duration = 10f;
    [SerializeField] private bool slowsEnemies = false;
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private bool extendsPickupRadius = false;
    [SerializeField] private bool isSingularity = false;
    [SerializeField] private GameObject fieldVisualPrefab;
    [SerializeField] private GameObject singularityVisualPrefab;
    [SerializeField] private GameObject tickParticlePrefab;

    private Coroutine activeFieldCoroutine;
    private GameObject activeVisual;
    private GameObject activeParticleEmitter;
    private bool fieldIsActive = false;

    protected override void Activate()
    {
        if (activeFieldCoroutine != null)
            StopCoroutine(activeFieldCoroutine);
        CleanupField();
        activeFieldCoroutine = StartCoroutine(FieldRoutine());
    }

    private void CleanupField()
    {
        if (activeVisual != null) { Destroy(activeVisual); activeVisual = null; }
        if (activeParticleEmitter != null) { Destroy(activeParticleEmitter); activeParticleEmitter = null; }
        fieldIsActive = false;
    }

    private IEnumerator FieldRoutine()
    {
        fieldIsActive = true;

        // Choose visual based on singularity
        GameObject visualPrefab = (isSingularity && singularityVisualPrefab != null)
            ? singularityVisualPrefab
            : fieldVisualPrefab;

        if (visualPrefab != null)
        {
            activeVisual = Instantiate(visualPrefab, transform.position, Quaternion.identity, transform);
            activeVisual.transform.localScale = Vector3.one * radius * 2f;
        }

        if (tickParticlePrefab != null)
        {
            activeParticleEmitter = Instantiate(tickParticlePrefab, transform.position, Quaternion.identity);
            activeParticleEmitter.transform.SetParent(transform);
            activeParticleEmitter.transform.localScale = Vector3.one * 0.045f;
        }

        if (isSingularity)
            yield return StartCoroutine(SingularityRoutine());

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += tickInterval;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (Collider2D hit in hits)
            {
                // Skip player and player children
                if (hit.CompareTag("Player"))
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damagePerTick);

                if (slowsEnemies)
                {
                    Enemy enemy = hit.GetComponentInParent<Enemy>();
                    if (enemy != null)
                        enemy.ApplySlow(slowMultiplier, tickInterval);
                }
            }

            yield return new WaitForSeconds(tickInterval);
        }

        CleanupField();
        activeFieldCoroutine = null;
    }

    private IEnumerator SingularityRoutine()
    {
        // Temporarily disable enemy collision with player
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer >= 0 && enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float pullDuration = 0.6f;
        float elapsed = 0f;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius * 2f);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                    continue;

                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null)
                    enemy.PullToward(transform.position, 8f);
            }

            yield return null;
        }

        // Re-enable collision
        if (playerLayer >= 0 && enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        // Small delay before explosion so enemies settle
        yield return new WaitForSeconds(0.1f);

        Collider2D[] explosionHits = Physics2D.OverlapCircleAll(transform.position, radius * 1.5f);
        foreach (Collider2D hit in explosionHits)
        {
            if (hit.CompareTag("Player"))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damagePerTick * 5);
        }
    }

    protected override void OnUpgraded()
    {
        if (definition == null) return;

        if (definition.VfxPrefabA != null) fieldVisualPrefab = definition.VfxPrefabA;
        if (definition.VfxPrefabB != null) tickParticlePrefab = definition.VfxPrefabB;
        if (definition.VfxPrefabC != null) singularityVisualPrefab = definition.VfxPrefabC;

        // Stats accumulate based on star level
        switch (definition.StarLevel)
        {
            case 0: // Base
                radius = 2f;
                damagePerTick = 1;
                tickInterval = 0.25f;
                duration = 10f;
                slowsEnemies = false;
                slowMultiplier = 0.5f;
                extendsPickupRadius = false;
                isSingularity = false;
                break;
            case 1: // +radius
                radius = 2.5f;
                duration = 10f;
                break;
            case 2: // +damage
                damagePerTick = 2;
                radius = 2.5f;
                duration = 12f;
                break;
            case 3: // slow
                slowsEnemies = true;
                damagePerTick = 2;
                radius = 2.5f;
                duration = 12f;
                break;
            case 4: // pickup radius
                extendsPickupRadius = true;
                slowsEnemies = true;
                damagePerTick = 2;
                radius = 2.5f;
                duration = 14f;
                break;
            case 5: // Singularity - all maxed
                isSingularity = true;
                radius = 4f;
                damagePerTick = 3;
                slowsEnemies = true;
                extendsPickupRadius = true;
                slowMultiplier = 0.3f;
                duration = 15f;
                break;
        }

        if (fieldIsActive)
            Activate();
    }

    public float GetRadius() => radius;
    public bool ExtendsPickupRadius() => extendsPickupRadius;
}