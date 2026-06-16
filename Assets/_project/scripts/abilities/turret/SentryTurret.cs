using System;
using System.Collections;
using UnityEngine;

public sealed class SentryTurret : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem boostParticles;

    private float maxHealth = 10f;
    private float currentHealth;
    private float fireCooldown = 1f;
    private float baseFirCooldown = 1f;
    private float nextFireTime;
    private float detectionRadius = 6f;
    private Projectile projectilePrefab;
    private int projectileDamage = 1;
    private float projectileSpeed = 10f;
    private float projectileLifetime = 2f;
    private LayerMask hitMask;

    private bool isRicochet = false;
    private int ricochetCount = 0;

    private bool hasStunPulse = false;
    private float stunPulseInterval = 3f;
    private float stunPulseRadius = 3f;
    private float stunDuration = 1f;
    private float nextStunTime;

    private int fireDirections = 1;
    private float fireSpreadAngle = 0f;
    private GameObject ownerObject;
    private Action onDestroyed;

    private bool isBoosted = false;

    public void Configure(
        float health, float cooldown, float detection,
        Projectile projPrefab, int damage, float speed, float lifetime, LayerMask mask,
        bool ricochet, int ricochetCnt,
        bool stunPulse, float stunInterval, float stunRadius, float stunDur,
        int fireDirs, float spreadAngle, GameObject owner,
        Action destroyedCallback)
    {
        maxHealth = health;
        currentHealth = maxHealth;
        fireCooldown = cooldown;
        baseFirCooldown = cooldown;
        detectionRadius = detection;
        projectilePrefab = projPrefab;
        projectileDamage = damage;
        projectileSpeed = speed;
        projectileLifetime = lifetime;
        hitMask = mask;
        isRicochet = ricochet;
        ricochetCount = ricochetCnt;
        hasStunPulse = stunPulse;
        stunPulseInterval = stunInterval;
        stunPulseRadius = stunRadius;
        stunDuration = stunDur;
        fireDirections  = fireDirs;
        fireSpreadAngle = spreadAngle;
        ownerObject = owner;
        onDestroyed = destroyedCallback;

        nextFireTime = Time.time;
        nextStunTime = Time.time + stunPulseInterval;

        if (boostParticles != null)
            boostParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        if (hasStunPulse && Time.time >= nextStunTime)
        {
            nextStunTime = Time.time + stunPulseInterval;
            StunPulse();
        }

        if (Time.time < nextFireTime)
            return;

        Transform target = FindNearestEnemy();
        if (target == null)
            return;

        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;

        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0f;

        Fire(direction);
        nextFireTime = Time.time + fireCooldown;
    }

    public void TriggerSpeedBoost(float duration, float multiplier = 0.5f)
    {
        if (isBoosted)
            StopAllCoroutines();
        StartCoroutine(SpeedBoostRoutine(duration, multiplier));
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        isBoosted = true;
        fireCooldown = baseFirCooldown * multiplier;

        if (boostParticles != null)
        {
            boostParticles.Clear();
            boostParticles.Play();
        }

        yield return new WaitForSeconds(duration);

        fireCooldown = baseFirCooldown;
        isBoosted = false;

        if (boostParticles != null)
            boostParticles.Stop();
    }

    public void TriggerManualStunPulse()
    {
        StunPulse();
        nextStunTime = Time.time + stunPulseInterval;
    }

    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsDead)
                continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void Fire(Vector2 baseDirection)
    {
        if (projectilePrefab == null)
            return;

        Vector3 firePos = firePoint != null ? firePoint.position : transform.position;

        if (fireDirections <= 1)
        {
            SpawnProjectile(baseDirection, firePos);
        }
        else if (fireDirections == 2 && fireSpreadAngle > 0f)
        {
            // Dual barrel: two shots at ±half spread aimed at the target
            float baseAngle  = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
            float halfSpread = fireSpreadAngle * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                float angle = baseAngle + (i == 0 ? -halfSpread : halfSpread);
                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );
                SpawnProjectile(dir, firePos);
            }
        }
        else
        {
            // Omnidirectional (3+): distribute evenly across 360°
            float angleStep = 360f / fireDirections;
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            for (int i = 0; i < fireDirections; i++)
            {
                float angle = baseAngle + angleStep * i;
                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );
                SpawnProjectile(dir, firePos);
            }
        }
    }

    private void SpawnProjectile(Vector2 direction, Vector3 firePos)
    {
        Projectile projectile = Instantiate(projectilePrefab, firePos, Quaternion.identity);
        projectile.Initialize(direction, projectileSpeed, projectileLifetime, projectileDamage, gameObject, hitMask, false);
        projectile.transform.right = direction;

        if (isRicochet)
            projectile.SetRicochet(ricochetCount);
    }

    private void StunPulse()
    {
        EMPRingEffect.Spawn(transform.position, stunPulseRadius);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stunPulseRadius);
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
                enemy.ApplySlow(0f, stunDuration);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            onDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}
