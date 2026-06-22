using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Weapon Catalog", fileName = "WeaponCatalog")]
public sealed class WeaponCatalog : ScriptableObject
{
    [SerializeField] private List<GunProfile> weaponProfiles = new List<GunProfile>();

    public IReadOnlyList<GunProfile> WeaponProfiles => weaponProfiles;

    public List<GunProfile> GetRandomChoices(int count)
    {
        var guaranteed = new List<GunProfile>();
        var pool       = new List<GunProfile>();

        for (int i = 0; i < weaponProfiles.Count; i++)
        {
            if (weaponProfiles[i] == null) continue;
            if (!weaponProfiles[i].IsUnlocked) continue;   // skip locked weapons
            if (weaponProfiles[i].AlwaysOffer) guaranteed.Add(weaponProfiles[i]);
            else                               pool.Add(weaponProfiles[i]);
        }

        var result = new List<GunProfile>();

        // Always-offer guns first (up to count)
        for (int i = 0; i < guaranteed.Count && result.Count < count; i++)
            result.Add(guaranteed[i]);

        // Fill remaining slots randomly
        int remaining = count - result.Count;
        int targetCount = Mathf.Clamp(remaining, 0, pool.Count);
        for (int i = 0; i < targetCount; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}