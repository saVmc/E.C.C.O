using System.Collections;
using UnityEngine;

public sealed class SentryAbility : Ability
{
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float turretHealth = 10f;
    [SerializeField] private float fireCooldown = 1f;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private int projectileDamage = 3;
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
    [SerializeField] private float fireSpreadAngle = 0f;
    [SerializeField] private float placeDistance = 1.5f;

    private bool hasSpeedBoost = false;
    private bool isOverclock   = false;
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
            if (isOverclock)
            {
                activeTurret.TriggerOverclock(BoostDuration);
                StartCoroutine(OverclockCameraShake());
                lastUsedTime = Time.time;
                return;
            }

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
                fireDirections, fireSpreadAngle, transform.root.gameObject,
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
        lastUsedTime = Time.time;
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
                turretHealth    = 15f;
                fireCooldown    = 0.8f;
                projectileDamage= 5;
                isRicochet      = false;
                hasStunPulse    = false;
                hasSpeedBoost   = false;
                isOverclock     = false;
                fireDirections  = 1;
                fireSpreadAngle = 0f;
                break;
            case 1:
                turretHealth = 22f;
                fireCooldown = 0.65f;
                hasSpeedBoost = true;
                break;
            case 2:
                fireDirections  = 2;
                fireSpreadAngle = 28f;
                break;
            case 3:
                projectileLifetime = 999f;
                isRicochet         = true;
                ricochetCount      = 1;
                hasStunPulse       = true;
                stunPulseInterval  = 3f;
                stunPulseRadius    = 3f;
                stunDuration       = 1f;
                break;
            case 4:
                ricochetCount = 3;
                break;
            case 5:
                fireDirections    = 3;
                ricochetCount     = 5;
                hasStunPulse      = true;
                stunPulseInterval = 1.5f;
                turretHealth      = 50f;
                fireCooldown      = 0.35f;
                hasSpeedBoost     = true;
                isOverclock       = true;
                break;
        }

        if (activeTurret != null)
        {
            activeTurret.Configure(
                turretHealth, fireCooldown, detectionRadius,
                projectilePrefab, projectileDamage, projectileSpeed, projectileLifetime, hitMask,
                isRicochet, ricochetCount,
                hasStunPulse, stunPulseInterval, stunPulseRadius, stunDuration,
                fireDirections, fireSpreadAngle, transform.root.gameObject,
                OnTurretDestroyed
            );
        }
    }

    private IEnumerator OverclockCameraShake()
    {
        yield return new WaitForSecondsRealtime(0.20f); // let the surge fire first
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 basePos = cam.transform.position;
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            float rm = 0.20f * (1f - elapsed / 0.55f);
            rm *= rm;
            cam.transform.position = basePos + new Vector3(
                UnityEngine.Random.Range(-rm, rm),
                UnityEngine.Random.Range(-rm, rm), 0f);
            yield return null;
        }
        cam.transform.position = basePos;
    }
}