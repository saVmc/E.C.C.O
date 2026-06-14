using UnityEngine;

public class AbilityDebugSetup : MonoBehaviour
{
    [SerializeField] private AbilityDefinition dashDefinition;
    [SerializeField] private AbilityDefinition forcefieldDefinition;

    private void Start()
    {
        // Setup Dash
        DashAbility dash = GetComponent<DashAbility>();
        if (dash != null && dashDefinition != null)
        {
            dash.Initialise(dashDefinition);
            if (AbilityManager.Instance != null)
                AbilityManager.Instance.TryAddAbility(dash);
        }
        else
        {
            if (dash == null) Debug.LogWarning("No DashAbility found on player.");
            if (dashDefinition == null) Debug.LogWarning("No dashDefinition assigned in Inspector.");
        }

        // Setup Forcefield
        ForcefieldAbility forcefield = GetComponent<ForcefieldAbility>();
        if (forcefield != null && forcefieldDefinition != null)
        {
            forcefield.Initialise(forcefieldDefinition);
            if (AbilityManager.Instance != null)
                AbilityManager.Instance.TryAddAbility(forcefield);
        }
        // Don't warn if forcefield is missing - it's optional for testing
    }
}