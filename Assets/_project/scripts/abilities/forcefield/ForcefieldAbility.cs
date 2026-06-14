using System.Collections;
using UnityEngine;

public sealed class ForcefieldAbility : Ability
{
    [Header("Forcefield Settings")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool slowsEnemies = false;
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private bool extendsPickupRadius = false;
    [SerializeField] private bool isSingularity = false;
    [SerializeField] private GameObject fieldVisualPrefab;

    protected override void Activate()
    {
        StartCoroutine(FieldRoutine());
    }

    private IEnumerator FieldRoutine()
    {
        GameObject visual = null;
        if (fieldVisualPrefab != null)
        {
            visual = Instantiate(fieldVisualPrefab, transform.position, Quaternion.identity, transform);
            visual.transform.localScale = Vector3.one * radius * 2f;
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

        if (visual != null)
            Destroy(visual);
    }

    private IEnumerator SingularityRoutine()
    {
        float pullDuration = 0.6f;
        float elapsed = 0f;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius * 2f);
            foreach (Collider2D hit in hits)
            {
                Enemy enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null)
                    enemy.PullToward(transform.position, 8f);
            }

            yield return null;
        }

        Collider2D[] explosionHits = Physics2D.OverlapCircleAll(transform.position, radius * 1.5f);
        foreach (Collider2D hit in explosionHits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damagePerTick * 5);
        }
    }

    protected override void OnUpgraded()
    {
        if (definition == null) return;

        // Stats accumulate based on star level
        switch (definition.StarLevel)
        {
            case 0: // Base
                radius = 2f;
                damagePerTick = 1;
                tickInterval = 0.25f;
                duration = 1f;
                slowsEnemies = false;
                slowMultiplier = 0.5f;
                extendsPickupRadius = false;
                isSingularity = false;
                break;
            case 1: // +radius
                radius = 2.5f;
                break;
            case 2: // +damage
                damagePerTick = 2;
                radius = 2.5f;
                break;
            case 3: // slow
                slowsEnemies = true;
                damagePerTick = 2;
                radius = 2.5f;
                break;
            case 4: // pickup radius
                extendsPickupRadius = true;
                slowsEnemies = true;
                damagePerTick = 2;
                radius = 2.5f;
                break;
            case 5: // Singularity - all maxed
                isSingularity = true;
                radius = 4f;
                damagePerTick = 3;
                slowsEnemies = true;
                extendsPickupRadius = true;
                slowMultiplier = 0.3f;
                break;
        }
    }

    public float GetRadius() => radius;
    public bool ExtendsPickupRadius() => extendsPickupRadius;
}