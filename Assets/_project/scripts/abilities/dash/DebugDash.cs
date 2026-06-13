using UnityEngine;

public class AbilityDebugSetup : MonoBehaviour
{
    [SerializeField] private AbilityDefinition dashDefinition;

    private void Start()
    {
        DashAbility dash = GetComponent<DashAbility>();
        if (dash == null)
        {
            Debug.LogWarning("No DashAbility found on player.");
            return;
        }

        dash.Initialise(dashDefinition);

        if (AbilityManager.Instance != null)
            AbilityManager.Instance.TryAddAbility(dash);
        else
            Debug.LogWarning("AbilityManager not found.");
    }
}