using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Gun Upgrade", fileName = "GunUpgrade")]
public sealed class GunUpgrade : ScriptableObject
{
    [SerializeField] private string displayName = "Upgrade";
    [SerializeField] private float fireCooldownMultiplier = 1f;
    [SerializeField] private float projectileSpeedMultiplier = 1f;
    [SerializeField] private float projectileLifetimeMultiplier = 1f;
    [SerializeField] private int projectileDamageBonus = 0;
    [SerializeField] private int magazineSizeBonus = 0;
    [SerializeField] private float reloadTimeMultiplier = 1f;
    [SerializeField] private Vector2 firePointOffsetDelta = Vector2.zero;
    [SerializeField] private Vector2 aimPivotOffsetDelta = Vector2.zero;

    [Header("Special Behaviour")]
    [SerializeField] private bool isTripleShot = false;
    [SerializeField] private bool isDoubleBarrel = false;
    [SerializeField] private int pelletCountBonus = 0;
    [SerializeField] private float spreadAngleDelta = 0f;
    [SerializeField] private bool isRicochet = false;
    [SerializeField] private int ricochetCount = 1;
    [SerializeField] private bool isInfiniteMag = false;
    [SerializeField] private bool enablesBulletTrail = false;
    [SerializeField] private bool isPiercing = false;
    [SerializeField] private int pierceCount = 2;

    [Header("Explosive")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private float explosionRadius = 2f;

    [Header("Executioner")]
    [SerializeField] private bool isExecutioner = false;
    [SerializeField] private float executionThreshold = 0.2f;

    [Header("Slow on Hit")]
    [SerializeField] private bool slowsEnemies = false;
    [SerializeField] private float slowMultiplier = 0.4f;
    [SerializeField] private float slowDuration = 2f;
    [Tooltip("0 = every bullet. N = only every Nth bullet gets the slow + special tint.")]
    [SerializeField] private int slowEveryNthBullet = 0;
    [SerializeField] private Color periodicSlowTint = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Mark Enemy")]
    [SerializeField] private bool marksEnemies = false;
    [SerializeField] private float markDamageMultiplier = 2f;
    [SerializeField] private float markDuration = 8f;
    [Tooltip("0 = every bullet marks. N = only every Nth bullet charges and fires a mark.")]
    [SerializeField] private int markEveryNthBullet = 0;

    [Header("Chain Kill")]
    [SerializeField] private bool isChainKill = false;

    [Header("Shockwave on Kill")]
    [SerializeField] private bool shockwaveOnKill = false;
    [SerializeField] private float shockwaveRadius = 3f;
    [SerializeField] private float shockwaveDamage = 15f;
    [SerializeField] private bool shockwaveMarks = false;

    [Header("Ammo on Kill")]
    [SerializeField] private int ammoOnKill = 0;

    [Header("Speed Boost on Fire")]
    [SerializeField] private bool speedBoostOnFire = false;
    [SerializeField] private float speedBoostMultiplier = 1.6f;
    [SerializeField] private float speedBoostDuration = 1.5f;

    [Header("Suppressive Fire")]
    [SerializeField] private bool suppressiveFire = false;
    [SerializeField] private float suppressiveSlowMultiplier = 0.6f;
    [SerializeField] private float suppressiveRange = 5f;

    [Header("Info")]
    [SerializeField] private string description = "Improves your weapon.";
    [SerializeField] private Sprite icon;
    [SerializeField] private int starLevel = 0;
    [SerializeField] private GunUpgrade nextStarUpgrade;

    public string DisplayName => displayName;
    public float FireCooldownMultiplier => fireCooldownMultiplier;
    public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
    public float ProjectileLifetimeMultiplier => projectileLifetimeMultiplier;
    public int ProjectileDamageBonus => projectileDamageBonus;
    public int MagazineSizeBonus => magazineSizeBonus;
    public float ReloadTimeMultiplier => reloadTimeMultiplier;
    public Vector2 FirePointOffsetDelta => firePointOffsetDelta;
    public Vector2 AimPivotOffsetDelta => aimPivotOffsetDelta;
    public bool IsTripleShot => isTripleShot;
    public bool IsDoubleBarrel => isDoubleBarrel;
    public int PelletCountBonus => pelletCountBonus;
    public float SpreadAngleDelta => spreadAngleDelta;
    public bool IsRicochet => isRicochet;
    public int RicochetCount => ricochetCount;
    public bool IsInfiniteMag => isInfiniteMag;
    public bool EnablesBulletTrail => enablesBulletTrail;
    public bool IsPiercing => isPiercing;
    public int PierceCount => pierceCount;
    public bool IsExplosive => isExplosive;
    public float ExplosionRadius => explosionRadius;
    public bool IsExecutioner => isExecutioner;
    public float ExecutionThreshold => executionThreshold;
    public bool SlowsEnemies => slowsEnemies;
    public float SlowMultiplier => slowMultiplier;
    public float SlowDuration => slowDuration;
    public int SlowEveryNthBullet => slowEveryNthBullet;
    public Color PeriodicSlowTint => periodicSlowTint;
    public bool MarksEnemies => marksEnemies;
    public float MarkDamageMultiplier => markDamageMultiplier;
    public float MarkDuration => markDuration;
    public int MarkEveryNthBullet => markEveryNthBullet;
    public bool IsChainKill => isChainKill;
    public bool ShockwaveOnKill => shockwaveOnKill;
    public float ShockwaveRadius => shockwaveRadius;
    public float ShockwaveDamage => shockwaveDamage;
    public bool ShockwaveMarks => shockwaveMarks;
    public int AmmoOnKill => ammoOnKill;
    public bool SpeedBoostOnFire => speedBoostOnFire;
    public float SpeedBoostMultiplier => speedBoostMultiplier;
    public float SpeedBoostDuration => speedBoostDuration;
    public bool SuppressiveFire => suppressiveFire;
    public float SuppressiveSlowMultiplier => suppressiveSlowMultiplier;
    public float SuppressiveRange => suppressiveRange;
    public string Description => description;
    public Sprite Icon => icon;
    public int StarLevel => starLevel;
    public GunUpgrade NextStarUpgrade => nextStarUpgrade;
    public bool IsMaxStar => nextStarUpgrade == null;
}
