using UnityEngine;

public sealed class SentryAbility : Ability
{
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float turretHealth = 10f;
    [SerializeField] private float fireCooldown = 1f;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private LayerMask hitMask = ~0;

    [SerializeField] private bool isRicochet = false;
    [SerializeField] private int ricochetCount = 0;

    [SerializeField] private bool hasStunPulse = false;
    [SerializeField] private float stunPulseInterval = 3f;
    [SerializeField] private float stunPulseRadius = 3f;
    [SerializeField] private float stunDuration = 1f;

    [SerializeField] private int fireDirections = 1;
    [SerializeField] private float placeDistance = 1.5f;

    private bool hasSpeedBoost = false;
    private const float BoostDuration = 5f;

    private const float DeployedCooldown = 15f;
    private const float DestroyedCooldown = 45f;

    private float runtimeCooldown = DestroyedCooldown;
    private SentryTurret activeTurret;

    public override bool IsReady => Time.time >= lastUsedTime + runtimeCooldown;
    public override float CooldownProgress => Mathf.Clamp01((Time.time - lastUsedTime) / runtimeCooldown);

    protected override void Activate()
    {
        if (activeTurret != null)
        {
            if (hasSpeedBoost)
                activeTurret.TriggerSpeedBoost(BoostDuration);

            if (hasStunPulse)
                activeTurret.TriggerManualStunPulse();

            if (hasSpeedBoost || hasStunPulse)
                lastUsedTime = Time.time;

            return;
        }

        if (turretPrefab == null)
            return;

        Vector3 placePos = transform.position + transform.right * placeDistance;
        GameObject turretObj = Instantiate(turretPrefab, placePos, Quaternion.identity);
        SentryTurret turret = turretObj.GetComponent<SentryTurret>();

        if (turret != null)
        {
            turret.Configure(
                turretHealth, fireCooldown, detectionRadius,
                projectilePrefab, projectileDamage, projectileSpeed, projectileLifetime, hitMask,
                isRicochet, ricochetCount,
                hasStunPulse, stunPulseInterval, stunPulseRadius, stunDuration,
                fireDirections, transform.root.gameObject,
                OnTurretDestroyed
            );
            activeTurret = turret;
        }

        runtimeCooldown = DeployedCooldown;
    }

    private void OnTurretDestroyed()
    {
        activeTurret = null;
        runtimeCooldown = DestroyedCooldown;
        lastUsedTime = Time.time; // start the 45s cooldown from now
    }

    public override void TryActivate()
    {
        if (!IsReady)
            return;

        lastUsedTime = Time.time;
        Activate();
    }

    protected override void OnUpgraded()
    {
        if (definition == null) return;

        if (definition.VfxPrefabA != null) turretPrefab = definition.VfxPrefabA;
        if (definition.VfxPrefabB != null) projectilePrefab = definition.VfxPrefabB.GetComponent<Projectile>();

        switch (definition.StarLevel)
        {
            case 0:
                turretHealth = 10f;
                fireCooldown = 1f;
                isRicochet = false;
                hasStunPulse = false;
                hasSpeedBoost = false;
                fireDirections = 1;
                break;
            case 1:
                turretHealth = 18f;
                fireCooldown = 0.7f;
                hasSpeedBoost = true;
                break;
            case 2:
                isRicochet = true;
                ricochetCount = 1;
                break;
            case 3:
                hasStunPulse = true;
                stunPulseInterval = 3f;
                stunPulseRadius = 3f;
                stunDuration = 1f;
                break;
            case 4:
                ricochetCount = 3;
                break;
            case 5:
                fireDirections = 3;
                ricochetCount = 5;
                hasStunPulse = true;
                stunPulseInterval = 1.5f;
                turretHealth = 50f;
                fireCooldown = 0.2f;
                break;
        }

        // If a turret is already deployed, reconfigure it with the new stats
        if (activeTurret != null)
        {
            activeTurret.Configure(
                turretHealth, fireCooldown, detectionRadius,
                projectilePrefab, projectileDamage, projectileSpeed, projectileLifetime, hitMask,
                isRicochet, ricochetCount,
                hasStunPulse, stunPulseInterval, stunPulseRadius, stunDuration,
                fireDirections, transform.root.gameObject,
                OnTurretDestroyed
            );
        }
    }
}
