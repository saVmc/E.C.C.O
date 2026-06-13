using UnityEngine;

public sealed class UpgradeOffer
{
    public AbilityDefinition Definition { get; private set; }
    public Ability ExistingAbility { get; private set; }
    public bool IsNewAbility { get; private set; }
    public GunUpgradeOffer GunUpgrade { get; private set; }
    public bool IsGunUpgrade => GunUpgrade != null;

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
            Ability newAbility = playerObject.AddComponent(
                System.Type.GetType(Definition.AbilityName + "Ability")
            ) as Ability;

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