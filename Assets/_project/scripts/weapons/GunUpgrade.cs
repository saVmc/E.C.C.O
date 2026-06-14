using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Gun Upgrade", fileName = "GunUpgrade")]
public sealed class GunUpgrade : ScriptableObject
{
    [SerializeField] private string displayName = "Upgrade";
    [SerializeField] private float fireCooldownMultiplier = 0.9f;
    [SerializeField] private float projectileSpeedMultiplier = 1f;
    [SerializeField] private float projectileLifetimeMultiplier = 1f;
    [SerializeField] private int projectileDamageBonus = 0;
    [SerializeField] private int magazineSizeBonus = 0;
    [SerializeField] private float reloadTimeMultiplier = 1f;
    [SerializeField] private Vector2 firePointOffsetDelta = Vector2.zero;
    [SerializeField] private Vector2 aimPivotOffsetDelta = Vector2.zero;
    [Header("Special Behaviour")]
[SerializeField] private bool isTripleShot = false;
[SerializeField] private bool isExplosive = false;
[SerializeField] private float explosionRadius = 2f;
[SerializeField] private bool isExecutioner = false;
[SerializeField] private float executionThreshold = 0.2f;
[SerializeField] private bool isDoubleBarrel = false;
[SerializeField] private int pelletCountBonus = 0;
[SerializeField] private float spreadAngleDelta = 0f;
[SerializeField] private bool isRicochet = false;
[SerializeField] private int ricochetCount = 1;
[SerializeField] private bool isInfiniteMag = false;
[SerializeField] private bool enablesBulletTrail = false;

public bool EnablesBulletTrail => enablesBulletTrail;
public bool IsTripleShot => isTripleShot;
public bool IsExplosive => isExplosive;
public float ExplosionRadius => explosionRadius;
public bool IsExecutioner => isExecutioner;
public float ExecutionThreshold => executionThreshold;
public bool IsDoubleBarrel => isDoubleBarrel;
public int PelletCountBonus => pelletCountBonus;
public float SpreadAngleDelta => spreadAngleDelta;
public bool IsRicochet => isRicochet;
public int RicochetCount => ricochetCount;
public bool IsInfiniteMag => isInfiniteMag;
    
    [SerializeField] private int pierceCount = 2;

    [SerializeField] private bool isPiercing = false;
    [Header("Info")]
    [SerializeField] private string description = "Improves your weapon.";
    [SerializeField] private Sprite icon;
    [SerializeField] private int starLevel = 0;
    [SerializeField] private GunUpgrade nextStarUpgrade;

public int PierceCount => pierceCount;

public string Description => description;
public Sprite Icon => icon;
public int StarLevel => starLevel;
public GunUpgrade NextStarUpgrade => nextStarUpgrade;
public bool IsMaxStar => nextStarUpgrade == null;
public bool IsPiercing => isPiercing;


    public string DisplayName => displayName;
    public float FireCooldownMultiplier => fireCooldownMultiplier;
    public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
    public float ProjectileLifetimeMultiplier => projectileLifetimeMultiplier;
    public int ProjectileDamageBonus => projectileDamageBonus;
    public int MagazineSizeBonus => magazineSizeBonus;
    public float ReloadTimeMultiplier => reloadTimeMultiplier;
    public Vector2 FirePointOffsetDelta => firePointOffsetDelta;
    public Vector2 AimPivotOffsetDelta => aimPivotOffsetDelta;
}