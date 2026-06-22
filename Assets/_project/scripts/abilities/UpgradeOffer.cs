using UnityEngine;

public sealed class UpgradeOffer
{
    public AbilityDefinition Definition { get; private set; }
    public Ability ExistingAbility { get; private set; }
    public bool IsNewAbility { get; private set; }
    public GunUpgradeOffer GunUpgrade { get; private set; }
    public bool IsGunUpgrade => GunUpgrade != null;
    public bool IsHealOffer { get; private set; }
    public PrestigeUpgradeType? PrestigeType { get; private set; }
    public bool IsPrestigeOffer => PrestigeType.HasValue;

    public static UpgradeOffer MakeHealOffer()
    {
        return new UpgradeOffer(null, null, false) { IsHealOffer = true };
    }

    public static UpgradeOffer MakePrestigeOffer(PrestigeUpgradeType type)
    {
        return new UpgradeOffer(null, null, false) { PrestigeType = type };
    }

    public UpgradeOffer(AbilityDefinition definition, Ability existingAbility, bool isNewAbility, GunUpgradeOffer gunUpgrade = null)
    {
        Definition = definition;
        ExistingAbility = existingAbility;
        IsNewAbility = isNewAbility;
        GunUpgrade = gunUpgrade;
    }

    public void Apply(AbilityManager manager, GameObject playerObject)
    {
        if (IsGunUpgrade)
        {
            Gun activeGun = playerObject.GetComponentInChildren<Gun>();
            GunUpgrade.Apply(activeGun);
            return;
        }

        if (IsNewAbility)
        {
            System.Type abilityType = System.Type.GetType(Definition.AbilityName + "Ability");
            if (abilityType == null) return;

            // Reuse an existing component of this type if one is already on the object
            // (e.g. manually placed in the Inspector with serialized fields assigned).
            Ability newAbility = playerObject.GetComponent(abilityType) as Ability;
            if (newAbility == null)
                newAbility = playerObject.AddComponent(abilityType) as Ability;

            if (newAbility != null)
            {
                newAbility.Initialise(Definition);
                manager.TryAddAbility(newAbility);
            }
        }
        else
        {
            ExistingAbility.Upgrade(Definition);
        }
    }
}