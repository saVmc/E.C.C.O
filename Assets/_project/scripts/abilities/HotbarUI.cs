using System.Collections.Generic;
using UnityEngine;

public sealed class AbilityHotbarUI : MonoBehaviour
{
    [SerializeField] private List<AbilityHotbarSlotUI> slotUIs = new List<AbilityHotbarSlotUI>();

    private void Start()
    {
        if (AbilityManager.Instance == null)
        {
            Debug.LogWarning("AbilityHotbarUI: AbilityManager not found.");
            return;
        }

        List<AbilitySlot> slots = AbilityManager.Instance.Slots;

        for (int i = 0; i < slotUIs.Count && i < slots.Count; i++)
            slotUIs[i].Initialise(slots[i]);
    }
}