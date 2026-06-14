using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    [SerializeField] private int maxSlots = 4;
    [SerializeField] private List<AbilityDefinition> availableAbilityPool = new List<AbilityDefinition>();

    private List<AbilitySlot> slots = new List<AbilitySlot>();
    private static readonly KeyCode[] hotkeys = { KeyCode.Q, KeyCode.E, KeyCode.C, KeyCode.F };

    public int MaxSlots => maxSlots;
    public List<AbilitySlot> Slots => slots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < maxSlots && i < hotkeys.Length; i++)
            slots.Add(new AbilitySlot(hotkeys[i]));
    }

    private void Update()
    {
        foreach (AbilitySlot slot in slots)
        {
            if (!slot.IsEmpty && IsHotkeyPressed(slot.HotKey))
                slot.Ability.TryActivate();
        }
    }

    private bool IsHotkeyPressed(KeyCode key)
    {
        return key switch
        {
            KeyCode.Q => Keyboard.current.qKey.wasPressedThisFrame,
            KeyCode.E => Keyboard.current.eKey.wasPressedThisFrame,
            KeyCode.C => Keyboard.current.cKey.wasPressedThisFrame,
            KeyCode.F => Keyboard.current.fKey.wasPressedThisFrame,
            KeyCode.Alpha1 => Keyboard.current.digit1Key.wasPressedThisFrame,
            KeyCode.Alpha2 => Keyboard.current.digit2Key.wasPressedThisFrame,
            KeyCode.Alpha3 => Keyboard.current.digit3Key.wasPressedThisFrame,
            KeyCode.Alpha4 => Keyboard.current.digit4Key.wasPressedThisFrame,
            _ => false
        };
    }

    public bool TryAddAbility(Ability ability)
    {
        foreach (AbilitySlot slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.AssignAbility(ability);
                return true;
            }
        }
        return false;
    }

    public bool HasAbilityOfType<T>() where T : Ability
    {
        foreach (AbilitySlot slot in slots)
            if (!slot.IsEmpty && slot.Ability is T)
                return true;
        return false;
    }

    public Ability GetAbilityOfType<T>() where T : Ability
    {
        foreach (AbilitySlot slot in slots)
            if (!slot.IsEmpty && slot.Ability is T)
                return slot.Ability;
        return null;
    }

    public bool AllSlotsFull()
    {
        foreach (AbilitySlot slot in slots)
            if (slot.IsEmpty) return false;
        return true;
    }

    public List<UpgradeOffer> GenerateUpgradeOffers(int count = 3)
{
    List<UpgradeOffer> allOffers = new List<UpgradeOffer>();

    Debug.Log($"[GenerateUpgradeOffers] === START GENERATION ===");
    Debug.Log($"[GenerateUpgradeOffers] Pool size: {availableAbilityPool.Count}, Slots full: {AllSlotsFull()}");
    
    for (int i = 0; i < slots.Count; i++)
    {
        if (!slots[i].IsEmpty)
            Debug.Log($"  Slot {i}: {slots[i].Ability.GetAbilityName()}, Star {slots[i].Ability.GetStarLevel()}, IsMaxStar: {slots[i].Ability.Definition.IsMaxStar}");
        else
            Debug.Log($"  Slot {i}: EMPTY");
    }

    // Ability upgrades (only if not max star)
    foreach (AbilitySlot slot in slots)
    {
        if (!slot.IsEmpty && !slot.Ability.Definition.IsMaxStar)
        {
            allOffers.Add(new UpgradeOffer(
                slot.Ability.Definition.NextStarDefinition,
                slot.Ability,
                false
            ));
            Debug.Log($"  + Added ability upgrade: {slot.Ability.Definition.NextStarDefinition.DisplayName} (to star {slot.Ability.Definition.NextStarDefinition.StarLevel})");
        }
        else if (!slot.IsEmpty && slot.Ability.Definition.IsMaxStar)
        {
            Debug.Log($"  - Skipped {slot.Ability.GetAbilityName()}: MAX STAR");
        }
    }

    Debug.Log($"  Ability upgrades total: {allOffers.Count}");

    // New abilities
    if (!AllSlotsFull())
    {
        Debug.Log($"  Checking for new abilities from pool of {availableAbilityPool.Count}:");
        foreach (AbilityDefinition def in availableAbilityPool)
        {
            bool alreadyHas = false;
            foreach (AbilitySlot slot in slots)
            {
                if (!slot.IsEmpty && slot.Ability.Definition.AbilityName == def.AbilityName)
                {
                    alreadyHas = true;
                    Debug.Log($"    - {def.DisplayName}: ALREADY OWNED");
                    break;
                }
            }
            if (!alreadyHas)
            {
                allOffers.Add(new UpgradeOffer(def, null, true));
                Debug.Log($"    + {def.DisplayName}: ADDED as new ability");
            }
        }
    }
    else
    {
        Debug.Log($"  All slots full - skipping new abilities");
    }

    Debug.Log($"  After new abilities total: {allOffers.Count}");

    // Gun upgrade
    PlayerShooter shooter = FindAnyObjectByType<PlayerShooter>();
    if (shooter != null)
    {
        Gun gun = shooter.GetActiveGun();
        if (gun != null && gun.CurrentProfile != null)
        {
            int nextStar = gun.appliedUpgrades.Count + 1;
            Debug.Log($"  Gun: {gun.CurrentProfile.DisplayName}, Applied upgrades: {gun.appliedUpgrades.Count}, Next star: {nextStar}");
            GunUpgrade gunUpgrade = gun.CurrentProfile.GetUpgradeForStar(nextStar);
            if (gunUpgrade != null)
            {
                GunUpgradeOffer gunOffer = new GunUpgradeOffer(gunUpgrade, gun.CurrentProfile.DisplayName);
                allOffers.Add(new UpgradeOffer(null, null, false, gunOffer));
                Debug.Log($"  + Added gun upgrade: {gunUpgrade.DisplayName} (star {nextStar})");
            }
            else
            {
                Debug.Log($"  - No gun upgrade for star {nextStar}");
            }
        }
        else
        {
            Debug.Log($"  - Gun or profile is NULL");
        }
    }
    else
    {
        Debug.Log($"  - PlayerShooter not found");
    }

    Debug.Log($"  === TOTAL OFFERS BEFORE SHUFFLE: {allOffers.Count} ===");

    Shuffle(allOffers);

    List<UpgradeOffer> result = new List<UpgradeOffer>();
    for (int i = 0; i < Mathf.Min(count, allOffers.Count); i++)
        result.Add(allOffers[i]);

    Debug.Log($"  Final result: {result.Count} offers");
    foreach (var offer in result)
        Debug.Log($"    Offer: {(offer.IsGunUpgrade ? offer.GunUpgrade.UpgradeName : offer.Definition?.DisplayName)}");

    return result;
}

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}