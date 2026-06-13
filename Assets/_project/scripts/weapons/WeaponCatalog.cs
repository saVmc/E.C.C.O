using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Weapon Catalog", fileName = "WeaponCatalog")]
public sealed class WeaponCatalog : ScriptableObject
{
    [SerializeField] private List<GunProfile> weaponProfiles = new List<GunProfile>();

    public IReadOnlyList<GunProfile> WeaponProfiles => weaponProfiles;

    public List<GunProfile> GetRandomChoices(int count)
    {
        List<GunProfile> availableProfiles = new List<GunProfile>();

        for (int i = 0; i < weaponProfiles.Count; i++)
        {
            if (weaponProfiles[i] != null)
                availableProfiles.Add(weaponProfiles[i]);
        }

        List<GunProfile> result = new List<GunProfile>();
        int targetCount = Mathf.Clamp(count, 0, availableProfiles.Count);

        for (int i = 0; i < targetCount; i++)
        {
            int index = Random.Range(0, availableProfiles.Count);
            result.Add(availableProfiles[index]);
            availableProfiles.RemoveAt(index);
        }

        return result;
    }
}