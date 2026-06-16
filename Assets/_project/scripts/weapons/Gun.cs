using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : MonoBehaviour
{
    [Header("Shot")]
    [SerializeField] protected Projectile projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float fireCooldown = 0.2f;
    [SerializeField] protected float projectileSpeed = 12f;
    [SerializeField] protected float projectileLifetime = 2f;
    [SerializeField] protected int projectileDamage = 1;
    [SerializeField] protected Color projectileTintColor = Color.white;
    [SerializeField] protected GameObject muzzleFlash;
    [SerializeField] protected float muzzleDuration = 0.08f;

    [Header("Ammo")]
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected float reloadTime = 1.1f;

    [Header("Aim")]
    [SerializeField] protected Transform aimPivot;
    [SerializeField] protected bool allowVerticalAim = true;

    [Header("Visuals")]
protected float projectileScale = 1f;
public float ProjectileScale => Mathf.Max(0.01f, projectileScale);
    [SerializeField] private SpriteRenderer gunSpriteRenderer;
    [SerializeField] private int weaponSortingOrderOffset = 5;

    protected bool isVisible = true;
    protected bool lastFacingLeft = false;
    [SerializeField] private float flipThreshold = 0.15f;
    [SerializeField] private float minFlipDistance = 0.5f;
    protected float recoilDistance = 0.08f;
    protected float recoilAngle = 6f;
    protected float recoilReturnSpeed = 22f;
    protected Vector2 defaultVisualOffset = new Vector2(0f, 0.12f);

    protected int ammoInMagazine;
    protected bool isReloading;
    protected ProjectileProfile currentProjectileProfile;
    protected float nextFireTime;
    protected bool firedThisFrame;
    protected Coroutine reloadRoutine;
    protected GunProfile currentProfile;
    public List<GunUpgrade> appliedUpgrades = new List<GunUpgrade>();
    protected Transform gunVisualTransform;
    protected SpriteRenderer playerSpriteRenderer;
    protected PlayerMovement playerMovement;
    protected PlayerAnimationDriver playerAnimationDriver;
    protected Vector3 visualRestLocalPosition;
    protected Quaternion visualRestLocalRotation = Quaternion.identity;
    protected int piercingCount = 2;
    protected Vector3 recoilOffset = Vector3.zero;
    protected float recoilRotation;
    protected float aimRotation;
    protected bool isTripleShot = false;
protected bool isExplosive = false;
protected float explosionRadius = 2f;
protected int explosiveEveryNthBullet = 0;
protected bool isExecutioner = false;
protected float executionThreshold = 0.2f;
protected bool isDoubleBarrel = false;
protected int pelletCountBonus = 0;
protected float spreadAngleDelta = 0f;
protected bool isRicochet = false;
protected int ricochetCount = 1;
protected bool gun5StarTrail = false;
protected bool isInfiniteMag = false;
    protected bool isPiercing = false;
    protected bool isBurstFire = false;
    protected int burstCount = 3;
    protected float burstDelay = 0.1f;
    protected bool slowsEnemies = false;
    protected float slowMultiplier = 0.4f;
    protected float slowDuration = 2f;
    protected int slowEveryNthBullet = 0;
    protected Color periodicSlowTint = new Color(0.2f, 0.9f, 1f, 1f);
    private int bulletsFiredTotal = 0;
    protected bool marksEnemies = false;
    protected float markDamageMultiplier = 2f;
    protected float markDuration = 8f;
    protected int markEveryNthBullet = 0;
    private int markBulletCounter = 0;
    private bool markIsReady = false;
    private float markGlowPulse = 0f;
    protected bool isChainKill = false;
    protected bool shockwaveOnKill = false;
    protected float shockwaveRadius = 3f;
    protected float shockwaveDamage = 15f;
    protected bool shockwaveMarks = false;
    protected int ammoOnKill = 0;
    protected bool speedBoostOnFire = false;
    protected float speedBoostMultiplier = 1.6f;
    protected float speedBoostDuration = 1.5f;
    protected bool suppressiveFire = false;
    protected float suppressiveSlowMultiplier = 0.6f;
    protected float suppressiveRange = 5f;
    protected bool burnsEnemies = false;
    protected int burnDamage = 4;
    protected int burnTicks = 3;
    protected float burnTickInterval = 1f;
    protected bool burnWildfire = false;
    protected float burnWildfireRadius = 2.5f;
    protected bool burnNapalm = false;
    protected float burnNapalmRadius = 2.5f;
    protected int burnNapalmDamage = 20;
    protected bool knockbackOnHit = false;
    protected float knockbackForce = 12f;

    public event Action OnShotFired;

    protected virtual void Awake()
    {
        if (firePoint == null) firePoint = FindChildByNames("FirePoint", "Muzzle");
        if (firePoint == null) firePoint = transform;

        if (aimPivot == null) aimPivot = FindChildByNames("AimPivot", "GunPivot", "FirePoint");
        if (aimPivot == null) aimPivot = transform;

        if (gunSpriteRenderer == null) gunSpriteRenderer = FindChildSpriteRendererByNames("GunVisuals", "GunSprite", "Visuals");
        if (gunSpriteRenderer == null) gunSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        playerSpriteRenderer = GetComponentInParent<SpriteRenderer>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerAnimationDriver = GetComponentInParent<PlayerAnimationDriver>();
        if (playerAnimationDriver == null)
        {
            playerAnimationDriver = transform.root.GetComponentInChildren<PlayerAnimationDriver>();
            if (playerAnimationDriver == null)
                Debug.LogWarning("PlayerAnimationDriver not found on Gun or its parent hierarchy.");
        }

        if (gunSpriteRenderer != null)
        {
            gunVisualTransform = gunSpriteRenderer.transform;
            visualRestLocalPosition = gunVisualTransform.localPosition;
            visualRestLocalRotation = gunVisualTransform.localRotation;
        }

        ApplyWeaponSortingOrder();

        ammoInMagazine = Mathf.Max(1, magazineSize);

        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    public void ShowGun()
    {
        if (!isVisible && gunSpriteRenderer != null)
        {
            isVisible = true;
            gunSpriteRenderer.enabled = true;
            if (muzzleFlash != null)
                muzzleFlash.SetActive(false);
        }
    }

    public void HideGun()
    {
        if (isVisible && gunSpriteRenderer != null)
        {
            isVisible = false;
            gunSpriteRenderer.enabled = false;
            if (muzzleFlash != null)
                muzzleFlash.SetActive(false);
        }
    }

    protected virtual void Update()
    {
        Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
        bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.001f;

        if (isMoving) HideGun(); else ShowGun();

        firedThisFrame = false;
        AimAtCursor();
        UpdateRecoil();
        UpdateMarkReadyGlow();
    }

    public void HandleInput(bool firePressed, bool reloadPressed)
    {
        if (reloadPressed)
            RequestReload();

        if (firePressed)
            TryFire();
    }

    public void TryFire()
    {
        if (!CanFire())
            return;

        Vector2 direction = GetAimDirection();
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        if (!allowVerticalAim)
        {
            direction.y = 0f;
            if (Mathf.Approximately(direction.x, 0f))
                direction.x = 1f;
        }

        Fire(direction.normalized);
    }

    public void RequestReload()
    {
        if (isReloading || ammoInMagazine >= magazineSize)
            return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

public virtual void ApplyProfile(GunProfile profile)
{
    if (profile == null)
        return;
    
    if (playerMovement != null)
        playerMovement.SetMoveSpeed(profile.PlayerMoveSpeed);

    currentProfile = profile;

    projectilePrefab = profile.ProjectilePrefab;
    currentProjectileProfile = profile.ProjectileProfile != null
        ? profile.ProjectileProfile.RuntimeCopy()
        : null;

    fireCooldown = profile.FireCooldown;

    if (currentProjectileProfile != null)
    {
        projectileSpeed = currentProjectileProfile.Speed;
        projectileLifetime = currentProjectileProfile.Lifetime;
        projectileDamage = currentProjectileProfile.Damage;
        projectileTintColor = currentProjectileProfile.TintColor;
        projectileScale = currentProjectileProfile.Scale;
    }

    magazineSize = Mathf.Max(1, profile.MagazineSize);
    reloadTime = profile.ReloadTime;
    allowVerticalAim = profile.AllowVerticalAim;

    if (gunSpriteRenderer == null)
        gunSpriteRenderer = FindChildSpriteRendererByNames("GunVisuals", "GunSprite", "Visuals");
    if (gunSpriteRenderer == null)
        gunSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

    if (gunSpriteRenderer != null)
    {
        if (gunVisualTransform == null)
            gunVisualTransform = gunSpriteRenderer.transform;
        if (profile.WeaponSprite != null)
            gunSpriteRenderer.sprite = profile.WeaponSprite;
    }

    ApplyWeaponSortingOrder();

    if (firePoint != null)
        firePoint.localPosition = profile.FirePointLocalPosition;
    if (aimPivot != null)
        aimPivot.localPosition = profile.AimPivotLocalPosition;

    if (gunVisualTransform != null)
    {
        gunVisualTransform.localScale = Vector3.one * profile.GunScale;
        gunVisualTransform.localPosition = profile.VisualLocalPosition;
        CacheVisualRestPose();
    }
    isBurstFire = profile.IsBurstFire;
    burstCount = profile.BurstCount;
    burstDelay = profile.BurstDelay;

    ammoInMagazine = magazineSize;
    isReloading = false;
    nextFireTime = 0f;
    recoilOffset = Vector3.zero;
    recoilRotation = 0f;
    bulletsFiredTotal = 0;
    markBulletCounter = 0;
    markIsReady = false;
    markGlowPulse = 0f;

    for (int i = 0; i < appliedUpgrades.Count; i++)
        ApplyUpgradeInternal(appliedUpgrades[i]);
}


    public virtual void ApplyUpgrade(GunUpgrade upgrade)
    {
        if (upgrade == null)
            return;
        if (upgrade.IsTripleShot) isTripleShot = true;
if (upgrade.IsExplosive) { isExplosive = true; explosionRadius = upgrade.ExplosionRadius; explosiveEveryNthBullet = upgrade.ExplosiveEveryNthBullet; }
if (upgrade.IsExecutioner) { isExecutioner = true; executionThreshold = upgrade.ExecutionThreshold; }
if (upgrade.IsDoubleBarrel) isDoubleBarrel = true;
if (upgrade.PelletCountBonus != 0) pelletCountBonus += upgrade.PelletCountBonus;
if (upgrade.SpreadAngleDelta != 0) spreadAngleDelta += upgrade.SpreadAngleDelta;
if (upgrade.IsRicochet) { isRicochet = true; ricochetCount = upgrade.RicochetCount; }
if (upgrade.IsInfiniteMag) isInfiniteMag = true;
        if (upgrade.SlowsEnemies) { slowsEnemies = true; slowMultiplier = upgrade.SlowMultiplier; slowDuration = upgrade.SlowDuration; slowEveryNthBullet = upgrade.SlowEveryNthBullet; periodicSlowTint = upgrade.PeriodicSlowTint; }
        if (upgrade.MarksEnemies) { marksEnemies = true; markDamageMultiplier = upgrade.MarkDamageMultiplier; markDuration = upgrade.MarkDuration; markEveryNthBullet = upgrade.MarkEveryNthBullet; }
        if (upgrade.IsChainKill) isChainKill = true;
        if (upgrade.ShockwaveOnKill) { shockwaveOnKill = true; shockwaveRadius = upgrade.ShockwaveRadius; shockwaveDamage = upgrade.ShockwaveDamage; shockwaveMarks = upgrade.ShockwaveMarks; }
        if (upgrade.AmmoOnKill > 0) ammoOnKill = upgrade.AmmoOnKill;
        if (upgrade.SpeedBoostOnFire) { speedBoostOnFire = true; speedBoostMultiplier = upgrade.SpeedBoostMultiplier; speedBoostDuration = upgrade.SpeedBoostDuration; }
        if (upgrade.SuppressiveFire) { suppressiveFire = true; suppressiveSlowMultiplier = upgrade.SuppressiveSlowMultiplier; suppressiveRange = upgrade.SuppressiveRange; if (DenialRing.Instance != null) DenialRing.Instance.SetRadius(suppressiveRange); }
        if (upgrade.BurnsEnemies || upgrade.BurnWildfire || upgrade.BurnNapalm) burnsEnemies = true;
        if (upgrade.BurnsEnemies) { burnDamage = upgrade.BurnDamage; burnTicks = upgrade.BurnTicks; burnTickInterval = upgrade.BurnTickInterval; }
        if (upgrade.KnockbackOnHit) { knockbackOnHit = true; knockbackForce = upgrade.KnockbackForce; }
        if (upgrade.BurnWildfire) { burnWildfire = true; burnWildfireRadius = upgrade.BurnWildfireRadius; }
        if (upgrade.BurnNapalm)   { burnNapalm = true; burnNapalmRadius = upgrade.BurnNapalmRadius; burnNapalmDamage = upgrade.BurnNapalmDamage; }

        appliedUpgrades.Add(upgrade);
        ApplyUpgradeInternal(upgrade);
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        ammoInMagazine = Mathf.Max(1, magazineSize);
        isReloading = false;
        reloadRoutine = null;
    }

    protected virtual void Fire(Vector2 direction)
{
    firedThisFrame = true;
    ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);
    nextFireTime = Time.time + fireCooldown;

    if (firePoint != null)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    int pellets = (currentProfile != null ? Mathf.Max(1, currentProfile.PelletCount) : 1) + pelletCountBonus;
    float spread = (currentProfile != null ? currentProfile.SpreadAngle : 0f) + spreadAngleDelta;

    LayerMask mask = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
    bool rotate = currentProjectileProfile != null && currentProjectileProfile.RotateToDirection;

    for (int i = 0; i < pellets; i++)
    {
        Vector2 pelletDirection = direction.normalized;

        if (pellets > 1 && spread > 0f)
        {
            float sliceSize = spread / pellets;
            float sliceStart = -spread / 2f + i * sliceSize;
            float angleOffset = sliceStart + UnityEngine.Random.Range(0f, sliceSize);
            float rad = angleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            pelletDirection = new Vector2(
                pelletDirection.x * cos - pelletDirection.y * sin,
                pelletDirection.x * sin + pelletDirection.y * cos
            ).normalized;
        }

        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Initialize(pelletDirection, projectileSpeed, projectileLifetime, projectileDamage, transform.root.gameObject, mask, rotate);

        if (currentProjectileProfile != null)
            projectile.ApplyProfile(currentProjectileProfile);
        projectile.transform.right = pelletDirection;

        if (gun5StarTrail)
            projectile.EnableTrail(projectileTintColor);

        if (isPiercing) projectile.SetPiercing(piercingCount);
        if (isExplosive && (explosiveEveryNthBullet <= 0 || bulletsFiredTotal % explosiveEveryNthBullet == 0)) projectile.SetExplosive(explosionRadius);
        if (isRicochet) projectile.SetRicochet(ricochetCount);
        if (isExecutioner) projectile.SetExecutioner(executionThreshold);
        ApplySlowToProjectile(projectile);
        ApplyMarkToProjectile(projectile);
        if (isChainKill) projectile.SetChainKill();
        if (shockwaveOnKill) projectile.SetShockwaveOnKill(shockwaveRadius, shockwaveDamage, shockwaveMarks);
        if (ammoOnKill > 0) projectile.SetAmmoOnKillCallback(OnProjectileKill);
        if (burnsEnemies) { projectile.SetBurn(burnDamage, burnTicks, burnTickInterval, burnWildfire, burnWildfireRadius, burnNapalm, burnNapalmRadius, burnNapalmDamage); projectile.SetScaleUp(0.15f, 0.1f); }
        if (knockbackOnHit) projectile.SetKnockback(knockbackForce);
        if (isDoubleBarrel)
    {
        float doubleBarrelDelay = 0.4f;
        StartCoroutine(FireDelayed(doubleBarrelDelay));
    }
        if (isTripleShot)
{
    Vector2 left = Quaternion.Euler(0, 0, 15f) * direction;
    Vector2 right = Quaternion.Euler(0, 0, -15f) * direction;
    foreach (Vector2 triDir in new[] { left, right })
    {
        Projectile tp = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        tp.Initialize(triDir.normalized, projectileSpeed, projectileLifetime, projectileDamage, transform.root.gameObject, mask, rotate);
        if (currentProjectileProfile != null) tp.ApplyProfile(currentProjectileProfile);
        tp.transform.right = triDir.normalized;
        if (isPiercing) tp.SetPiercing(piercingCount);
        if (isExplosive && (explosiveEveryNthBullet <= 0 || bulletsFiredTotal % explosiveEveryNthBullet == 0)) tp.SetExplosive(explosionRadius);
    }
}
    }

    if (isBurstFire && burstCount > 1)
        StartCoroutine(BurstFireRoutine(direction));

    if (speedBoostOnFire && playerMovement != null)
        playerMovement.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);

    if (suppressiveFire) ApplySuppressiveFire();

    ApplyRecoil(direction);
    OnShotFired?.Invoke();

    if (muzzleFlash != null)
    {
        muzzleFlash.transform.position = firePoint.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        muzzleFlash.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        StartCoroutine(MuzzleFlashCoroutine());
    }
}


    private IEnumerator BurstFireRoutine(Vector2 initialDirection)
    {
        LayerMask mask = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
        bool rotate = currentProjectileProfile != null && currentProjectileProfile.RotateToDirection;
        for (int shot = 1; shot < burstCount; shot++)
        {
            yield return new WaitForSeconds(burstDelay);
            if (projectilePrefab == null) yield break;
            Vector2 direction = GetAimDirection();
            if (direction.sqrMagnitude < 0.0001f) direction = initialDirection;
            if (!allowVerticalAim) { direction.y = 0f; if (Mathf.Approximately(direction.x, 0f)) direction.x = 1f; }
            direction = direction.normalized;
            ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);
            Projectile p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            p.Initialize(direction, projectileSpeed, projectileLifetime, projectileDamage, transform.root.gameObject, mask, rotate);
            if (currentProjectileProfile != null) p.ApplyProfile(currentProjectileProfile);
            p.transform.right = direction;
            if (gun5StarTrail) p.EnableTrail(projectileTintColor);
            if (isPiercing) p.SetPiercing(piercingCount);
            if (isExplosive && (explosiveEveryNthBullet <= 0 || bulletsFiredTotal % explosiveEveryNthBullet == 0)) p.SetExplosive(explosionRadius);
            if (isRicochet) p.SetRicochet(ricochetCount);
            if (isExecutioner) p.SetExecutioner(executionThreshold);
            ApplySlowToProjectile(p);
            ApplyMarkToProjectile(p);
            if (isChainKill) p.SetChainKill();
            if (shockwaveOnKill) p.SetShockwaveOnKill(shockwaveRadius, shockwaveDamage, shockwaveMarks);
            if (ammoOnKill > 0) p.SetAmmoOnKillCallback(OnProjectileKill);
            if (burnsEnemies) { p.SetBurn(burnDamage, burnTicks, burnTickInterval, burnWildfire, burnWildfireRadius, burnNapalm, burnNapalmRadius, burnNapalmDamage); p.SetScaleUp(0.15f, 0.1f); }
            if (knockbackOnHit) p.SetKnockback(knockbackForce);
            if (suppressiveFire) ApplySuppressiveFire();
            ApplyRecoil(direction);
            OnShotFired?.Invoke();
            if (ammoInMagazine <= 0) yield break;
        }
    }

    private IEnumerator FireDelayed(float delay)
{
    yield return new WaitForSeconds(delay);
    Vector2 direction = GetAimDirection();
    if (direction.sqrMagnitude < 0.0001f)
        direction = Vector2.right;
    if (!allowVerticalAim)
    {
        direction.y = 0f;
        if (Mathf.Approximately(direction.x, 0f))
            direction.x = 1f;
    }
    int pellets = (currentProfile != null ? Mathf.Max(1, currentProfile.PelletCount) : 1) + pelletCountBonus;
    float spread = (currentProfile != null ? currentProfile.SpreadAngle : 0f) + spreadAngleDelta;
    LayerMask mask = currentProjectileProfile != null ? currentProjectileProfile.HitMask : ~0;
    bool rotate = currentProjectileProfile != null && currentProjectileProfile.RotateToDirection;

    for (int i = 0; i < pellets; i++)
    {
        Vector2 pelletDirection = direction.normalized;
        if (pellets > 1 && spread > 0f)
        {
            float sliceSize = spread / pellets;
            float sliceStart = -spread / 2f + i * sliceSize;
            float angleOffset = sliceStart + UnityEngine.Random.Range(0f, sliceSize);
            float rad = angleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad); float sin = Mathf.Sin(rad);
            pelletDirection = new Vector2(pelletDirection.x * cos - pelletDirection.y * sin, pelletDirection.x * sin + pelletDirection.y * cos).normalized;
        }
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.Initialize(pelletDirection, projectileSpeed, projectileLifetime, projectileDamage, transform.root.gameObject, mask, rotate);
        if (currentProjectileProfile != null) projectile.ApplyProfile(currentProjectileProfile);
        projectile.transform.right = pelletDirection;
        if (isPiercing) projectile.SetPiercing(piercingCount);
        if (isExplosive && (explosiveEveryNthBullet <= 0 || bulletsFiredTotal % explosiveEveryNthBullet == 0)) projectile.SetExplosive(explosionRadius);
        ApplySlowToProjectile(projectile);
        ApplyMarkToProjectile(projectile);
        if (isChainKill) projectile.SetChainKill();
        if (shockwaveOnKill) projectile.SetShockwaveOnKill(shockwaveRadius, shockwaveDamage, shockwaveMarks);
        if (ammoOnKill > 0) projectile.SetAmmoOnKillCallback(OnProjectileKill);
        if (burnsEnemies) { projectile.SetBurn(burnDamage, burnTicks, burnTickInterval, burnWildfire, burnWildfireRadius, burnNapalm, burnNapalmRadius, burnNapalmDamage); projectile.SetScaleUp(0.15f, 0.1f); }
        if (knockbackOnHit) projectile.SetKnockback(knockbackForce);
    }
}

    protected virtual IEnumerator MuzzleFlashCoroutine()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(muzzleDuration);
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    public virtual void AimAtCursor()
    {
        Vector2 direction = GetAimDirection();
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float cursorDist = direction.magnitude;
        Vector2 normDirection = cursorDist > 0.0001f ? direction / cursorDist : Vector2.right;
        aimRotation = Mathf.Atan2(normDirection.y, normDirection.x) * Mathf.Rad2Deg;

        if (cursorDist >= minFlipDistance)
        {
            if (lastFacingLeft && normDirection.x > flipThreshold)
                lastFacingLeft = false;
            else if (!lastFacingLeft && normDirection.x < -flipThreshold)
                lastFacingLeft = true;
        }

        bool facingLeft = lastFacingLeft;

        if (aimPivot != null)
            aimPivot.localRotation = Quaternion.identity;

        if (gunSpriteRenderer != null)
            gunSpriteRenderer.flipY = facingLeft;

        if (playerAnimationDriver != null)
            playerAnimationDriver.SetAimFlip(facingLeft);

        if (gunVisualTransform != null && currentProfile != null)
        {
            Vector3 pos = currentProfile.VisualLocalPosition;
            if (facingLeft)
                pos.x = -pos.x;
            gunVisualTransform.localPosition = pos;
        }
    }

    public abstract Vector2 GetAimDirection();

    protected virtual void ApplyUpgradeInternal(GunUpgrade upgrade)
    {
        fireCooldown = Mathf.Max(0.01f, fireCooldown * upgrade.FireCooldownMultiplier);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed * upgrade.ProjectileSpeedMultiplier);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime * upgrade.ProjectileLifetimeMultiplier);
        projectileDamage = Mathf.Max(1, projectileDamage + upgrade.ProjectileDamageBonus);
        magazineSize = Mathf.Max(1, magazineSize + upgrade.MagazineSizeBonus);
        reloadTime = Mathf.Max(0.05f, reloadTime * upgrade.ReloadTimeMultiplier);

        if (firePoint != null)
            firePoint.localPosition += (Vector3)upgrade.FirePointOffsetDelta;
        

        if (aimPivot != null)
            aimPivot.localPosition += (Vector3)upgrade.AimPivotOffsetDelta;
        
        if (upgrade.IsPiercing)
{
    isPiercing = true;
    piercingCount = upgrade.PierceCount;
}
    }

    protected virtual void ApplyRecoil(Vector2 direction)
    {
        if (gunVisualTransform == null)
            return;

        float horizontalDirection = direction.x >= 0f ? -1f : 1f;
        recoilOffset = new Vector3(recoilDistance * horizontalDirection, 0f, 0f);
        recoilRotation = recoilAngle * horizontalDirection;
    }

    protected virtual void UpdateRecoil()
    {
        if (gunVisualTransform == null)
            return;

        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
        recoilRotation = Mathf.Lerp(recoilRotation, 0f, recoilReturnSpeed * Time.deltaTime);

        gunVisualTransform.localRotation = visualRestLocalRotation *
            Quaternion.Euler(0f, 0f, aimRotation + recoilRotation);
    }

    protected virtual void CacheVisualRestPose()
    {
        if (gunVisualTransform == null)
            return;

        visualRestLocalPosition = gunVisualTransform.localPosition + (Vector3)defaultVisualOffset;
        gunVisualTransform.localPosition = visualRestLocalPosition;
        // Always reset rotation so aimRotation isn't compounded when ApplyProfile runs mid-aim
        gunVisualTransform.localRotation = Quaternion.identity;
        visualRestLocalRotation = Quaternion.identity;
    }

    protected virtual void ApplyWeaponSortingOrder()
    {
        if (gunSpriteRenderer == null)
            return;

        if (playerSpriteRenderer != null)
        {
            gunSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            gunSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + weaponSortingOrderOffset;
            return;
        }

        if (gunSpriteRenderer.sortingOrder < weaponSortingOrderOffset)
            gunSpriteRenderer.sortingOrder = weaponSortingOrderOffset;
    }

    protected Transform FindChildByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = transform.Find(names[i]);
            if (child != null)
                return child;
        }

        return null;
    }

    protected SpriteRenderer FindChildSpriteRendererByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = transform.Find(names[i]);
            if (child != null)
            {
                SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    return spriteRenderer;

                spriteRenderer = child.GetComponentInChildren<SpriteRenderer>(true);
                if (spriteRenderer != null)
                    return spriteRenderer;
            }
        }

        return null;
    }

    private void ApplyMarkToProjectile(Projectile p)
    {
        if (!marksEnemies) return;
        if (markEveryNthBullet <= 0)
        {
            p.SetMark(markDamageMultiplier, markDuration);
            return;
        }
        if (markIsReady)
        {
            p.SetMark(markDamageMultiplier, markDuration);
            markIsReady = false;
            markBulletCounter = 0;
            markGlowPulse = 0f;
        }
        else
        {
            markBulletCounter++;
            if (markBulletCounter >= markEveryNthBullet)
                markIsReady = true;
        }
    }

    private void UpdateMarkReadyGlow()
    {
        if (!marksEnemies || markEveryNthBullet <= 0 || gunSpriteRenderer == null) return;
        if (markIsReady)
        {
            markGlowPulse += Time.deltaTime * 5f;
            float t = (Mathf.Sin(markGlowPulse) + 1f) * 0.5f;
            gunSpriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.1f, 1f), t * 0.8f);
        }
        else
        {
            gunSpriteRenderer.color = Color.white;
            markGlowPulse = 0f;
        }
    }

    private void ApplySlowToProjectile(Projectile p)
    {
        if (!slowsEnemies) return;
        bulletsFiredTotal++;
        bool isSlowBullet = slowEveryNthBullet <= 0 || bulletsFiredTotal % slowEveryNthBullet == 0;
        if (isSlowBullet)
        {
            p.SetSlow(slowMultiplier, slowDuration);
            p.SetTint(periodicSlowTint);
        }
    }

    private void OnProjectileKill()
    {
        ammoInMagazine = Mathf.Min(magazineSize, ammoInMagazine + ammoOnKill);
    }

    private void ApplySuppressiveFire()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.root.position, suppressiveRange);
        foreach (Collider2D col in nearby)
        {
            if (col.CompareTag("Player") || col.transform.root.CompareTag("Player")) continue;
            Enemy e = col.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead)
                e.ApplySlow(suppressiveSlowMultiplier, fireCooldown * 4f);
        }
    }

    protected bool CanFire()
{
    if (isInfiniteMag && playerMovement != null && playerMovement.GetMovementDirection().sqrMagnitude > 0.001f)
        return projectilePrefab != null && !isReloading && Time.time >= nextFireTime;

    return projectilePrefab != null && !isReloading && ammoInMagazine > 0 && Time.time >= nextFireTime;
}

    public bool LocksMovementWhileFiring => currentProfile != null && currentProfile.LocksMovementWhileFiring && !isInfiniteMag;
    public bool FiredThisFrame() => firedThisFrame;
    public bool IsReloading => isReloading;
    public int AmmoInMagazine => ammoInMagazine;
    public int MagazineSize => magazineSize;
    public Projectile GetProjectilePrefab() => projectilePrefab;
    public float GetReloadTime() => reloadTime;
    public float GetFireCooldown() => fireCooldown;
    public GunProfile CurrentProfile => currentProfile;
}