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

    // Ability upgrades
    foreach (AbilitySlot slot in slots)
    {
        if (!slot.IsEmpty && !slot.Ability.Definition.IsMaxStar)
        {
            allOffers.Add(new UpgradeOffer(
                slot.Ability.Definition.NextStarDefinition,
                slot.Ability,
                false
            ));
        }
    }

    // New abilities
    if (!AllSlotsFull())
    {
        foreach (AbilityDefinition def in availableAbilityPool)
        {
            bool alreadyHas = false;
            foreach (AbilitySlot slot in slots)
            {
                if (!slot.IsEmpty && slot.Ability.Definition.AbilityName == def.AbilityName)
                {
                    alreadyHas = true;
                    break;
                }
            }
            if (!alreadyHas)
                allOffers.Add(new UpgradeOffer(def, null, true));
        }
    }

    PlayerShooter shooter = FindAnyObjectByType<PlayerShooter>();
    if (shooter != null)
    {
        Gun gun = shooter.GetActiveGun();
        if (gun != null && gun.CurrentProfile != null)
        {
            int nextStar = gun.appliedUpgrades.Count + 1;
            GunUpgrade gunUpgrade = gun.CurrentProfile.GetUpgradeForStar(nextStar);
            if (gunUpgrade != null)
            {
                GunUpgradeOffer gunOffer = new GunUpgradeOffer(gunUpgrade, gun.CurrentProfile.DisplayName);
                allOffers.Add(new UpgradeOffer(null, null, false, gunOffer));
            }
        }
    }

    Shuffle(allOffers);

    List<UpgradeOffer> result = new List<UpgradeOffer>();
    for (int i = 0; i < Mathf.Min(count, allOffers.Count); i++)
        result.Add(allOffers[i]);

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