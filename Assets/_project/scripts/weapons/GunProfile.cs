using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Gun Profile", fileName = "GunProfile")]
public sealed class GunProfile : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName = "Gun";
    [SerializeField] private float gunScale = 1f;

    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private ProjectileProfile projectileProfile;
    [SerializeField] private Sprite weaponSprite;
    [Tooltip("Upscaled version of the sprite used on the gun selection card UI.")]
    [SerializeField] private Sprite cardIconSprite;

    [Header("Fire")]
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private float reloadTime = 1.1f;

    [Header("Aim")]
    [SerializeField] private bool allowVerticalAim = true;
    [SerializeField] private Vector2 firePointLocalPosition = new Vector2(0.55f, 0f);
    [SerializeField] private Vector2 aimPivotLocalPosition = Vector2.zero;
    [SerializeField] private Vector2 visualLocalPosition = Vector2.zero;

    [Header("Weight & Misc")]
    [SerializeField] private int weight = 1;
    [SerializeField] private Sprite ammoIcon;

    [Header("Burst / Spread")]
    [SerializeField] private int pelletCount = 1;
    [SerializeField] private float spreadAngle = 0f;

    [Header("Player Stats")]
    [SerializeField] private float playerMoveSpeed = 5f;

    [Header("Burst Fire")]
    [SerializeField] private bool isBurstFire = false;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;

    [Header("Movement")]
    [SerializeField] private bool locksMovementWhileFiring = false;

    [Header("Upgrades")]
    [SerializeField] private GunUpgrade starOneUpgrade;
    [SerializeField] private GunUpgrade starTwoUpgrade;
    [SerializeField] private GunUpgrade starThreeUpgrade;
    [SerializeField] private GunUpgrade starFourUpgrade;
    [SerializeField] private GunUpgrade starFiveUpgrade;

public GunUpgrade GetUpgradeForStar(int star)
{
    return star switch
    {
        1 => starOneUpgrade,
        2 => starTwoUpgrade,
        3 => starThreeUpgrade,
        4 => starFourUpgrade,
        5 => starFiveUpgrade,
        _ => null
    };
}
    public bool IsBurstFire => isBurstFire;
    public int BurstCount => Mathf.Max(1, burstCount);
    public float BurstDelay => burstDelay;
    public float PlayerMoveSpeed => playerMoveSpeed;
    public bool LocksMovementWhileFiring => locksMovementWhileFiring;

    public string DisplayName => displayName;
    public Projectile ProjectilePrefab => projectilePrefab;
    public ProjectileProfile ProjectileProfile => projectileProfile;
    public Sprite WeaponSprite => weaponSprite;
    public Sprite CardIconSprite => cardIconSprite != null ? cardIconSprite : weaponSprite;
    public float FireCooldown => fireCooldown;
    public float GunScale => Mathf.Max(0.01f, gunScale);
    public int MagazineSize => magazineSize;
    public float ReloadTime => reloadTime;
    public bool AllowVerticalAim => allowVerticalAim;
    public Vector2 FirePointLocalPosition => firePointLocalPosition;
    public Vector2 AimPivotLocalPosition => aimPivotLocalPosition;
    public Vector2 VisualLocalPosition => visualLocalPosition;
    public int Weight => Mathf.Max(1, weight);
    public Sprite AmmoIcon => ammoIcon;
    public int PelletCount => Mathf.Max(1, pelletCount);
    public float SpreadAngle => spreadAngle;
}