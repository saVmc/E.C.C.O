using UnityEngine;

public sealed class UpgradeOffer
{
    public AbilityDefinition Definition { get; private set; }
    public Ability ExistingAbility { get; private set; }
    public bool IsNewAbility { get; private set; }

    public UpgradeOffer(AbilityDefinition definition, Ability existingAbility, bool isNewAbility)
    {
        Definition = definition;
        ExistingAbility = existingAbility;
        IsNewAbility = isNewAbility;
    }

    public void Apply(AbilityManager manager, GameObject playerObject)
    {
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