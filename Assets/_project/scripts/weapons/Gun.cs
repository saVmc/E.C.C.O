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
protected bool isExecutioner = false;
protected float executionThreshold = 0.2f;
protected bool isDoubleBarrel = false;
protected int pelletCountBonus = 0;
protected float spreadAngleDelta = 0f;
protected bool isRicochet = false;
protected int ricochetCount = 1;
protected bool isInfiniteMag = false;
    protected bool isPiercing = false;

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

    if (gunSpriteRenderer != null && profile.WeaponSprite != null)
        gunSpriteRenderer.sprite = profile.WeaponSprite;

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
    ammoInMagazine = magazineSize;
    isReloading = false;
    nextFireTime = 0f;
    recoilOffset = Vector3.zero;
    recoilRotation = 0f;

    for (int i = 0; i < appliedUpgrades.Count; i++)
        ApplyUpgradeInternal(appliedUpgrades[i]);
}


    public virtual void ApplyUpgrade(GunUpgrade upgrade)
    {
        if (upgrade == null)
            return;
        if (upgrade.IsTripleShot) isTripleShot = true;
if (upgrade.IsExplosive) { isExplosive = true; explosionRadius = upgrade.ExplosionRadius; }
if (upgrade.IsExecutioner) { isExecutioner = true; executionThreshold = upgrade.ExecutionThreshold; }
if (upgrade.IsDoubleBarrel) isDoubleBarrel = true;
if (upgrade.PelletCountBonus != 0) pelletCountBonus += upgrade.PelletCountBonus;
if (upgrade.SpreadAngleDelta != 0) spreadAngleDelta += upgrade.SpreadAngleDelta;
if (upgrade.IsRicochet) { isRicochet = true; ricochetCount = upgrade.RicochetCount; }
if (upgrade.IsInfiniteMag) isInfiniteMag = true;

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

        if (isPiercing)
    projectile.SetPiercing(piercingCount);
        if (isExplosive)
    projectile.SetExplosive(explosionRadius);

if (isRicochet)
    projectile.SetRicochet(ricochetCount);

if (isExecutioner)
    projectile.SetExecutioner(executionThreshold);
        if (isDoubleBarrel)
{
    float doubleBarrelDelay = 0.08f;
    StartCoroutine(FireDelayed(direction, doubleBarrelDelay));
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
        if (isExplosive) tp.SetExplosive(explosionRadius);
    }
}
    }

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


    private IEnumerator FireDelayed(Vector2 direction, float delay)
{
    yield return new WaitForSeconds(delay);
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
        if (isExplosive) projectile.SetExplosive(explosionRadius);
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
        visualRestLocalRotation = gunVisualTransform.localRotation;
        gunVisualTransform.localPosition = visualRestLocalPosition;
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

    protected bool CanFire()
{
    if (isInfiniteMag && playerMovement != null && playerMovement.GetMovementDirection().sqrMagnitude > 0.001f)
        return projectilePrefab != null && !isReloading && Time.time >= nextFireTime;

    return projectilePrefab != null && !isReloading && ammoInMagazine > 0 && Time.time >= nextFireTime;
}

    public bool FiredThisFrame() => firedThisFrame;
    public bool IsReloading => isReloading;
    public int AmmoInMagazine => ammoInMagazine;
    public int MagazineSize => magazineSize;
    public Projectile GetProjectilePrefab() => projectilePrefab;
    public float GetReloadTime() => reloadTime;
    public float GetFireCooldown() => fireCooldown;
    public GunProfile CurrentProfile => currentProfile;
}