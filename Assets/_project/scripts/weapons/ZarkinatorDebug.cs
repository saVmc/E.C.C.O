using UnityEngine;

/// DEBUG ONLY — attach to any scene GameObject, assign zarkinatorProfile in Inspector.
/// F8  = equip Zarkinator + apply all 5 upgrades
/// F9  = apply next upgrade one at a time (step through ★1→★5)
/// F10 = re-equip at ★0 (fresh gun, no upgrades)
public sealed class ZarkinatorDebug : MonoBehaviour
{
    [SerializeField] private GunProfile zarkinatorProfile;

    private WeaponDraftManager draftManager;
    private PlayerShooter      playerShooter;
    private int                nextStar = 1;

    private void Awake()
    {
        draftManager  = FindAnyObjectByType<WeaponDraftManager>();
        playerShooter = FindAnyObjectByType<PlayerShooter>();
    }

    private void Update()
    {
        if (zarkinatorProfile == null) return;

        if (Input.GetKeyDown(KeyCode.F8))  EquipAndMaxOut();
        if (Input.GetKeyDown(KeyCode.F9))  ApplyNextUpgrade();
        if (Input.GetKeyDown(KeyCode.F10)) EquipFresh();
    }

    private void EquipFresh()
    {
        if (playerShooter == null) return;
        playerShooter.EquipWeapon(zarkinatorProfile);
        nextStar = 1;
        Debug.Log("[ZarkinatorDebug] Equipped fresh (no upgrades).");
    }

    private void ApplyNextUpgrade()
    {
        if (playerShooter == null) return;
        if (nextStar == 1) playerShooter.EquipWeapon(zarkinatorProfile);

        GunUpgrade up = zarkinatorProfile.GetUpgradeForStar(nextStar);
        if (up == null) { Debug.Log("[ZarkinatorDebug] Already at max star."); return; }

        playerShooter.ApplyUpgrade(up);
        Debug.Log($"[ZarkinatorDebug] Applied ★{nextStar}: {up.DisplayName}");
        nextStar++;
    }

    private void EquipAndMaxOut()
    {
        if (playerShooter == null) return;
        playerShooter.EquipWeapon(zarkinatorProfile);
        for (int star = 1; star <= 5; star++)
        {
            GunUpgrade up = zarkinatorProfile.GetUpgradeForStar(star);
            if (up != null) playerShooter.ApplyUpgrade(up);
        }
        nextStar = 6;
        Debug.Log("[ZarkinatorDebug] Equipped + all 5 upgrades applied.");
    }
}
