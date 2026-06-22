using System.Collections.Generic;
using UnityEngine;

public enum PrestigeUpgradeType
{
    Overclock,
    VitalSurge,
    Adrenaline,
    LastStand,
}

public static class PrestigeEffects
{
    // ── Per-run state ──────────────────────────────────────────────────────
    public static float CooldownMultiplier { get; private set; } = 1f;
    public static float SpeedMultiplier    { get; private set; } = 1f;
    public static bool  HasLastStand       { get; private set; } = false;
    public static bool  LastStandUsed      { get; private set; } = false;

    private static int prestigeStackCount = 0;

    // Enemy scaling: +15% health and +5% speed per prestige stack
    public static float EnemyHealthScale => 1f + prestigeStackCount * 0.15f;
    public static float EnemySpeedScale  => 1f + prestigeStackCount * 0.05f;

    private static readonly HashSet<PrestigeUpgradeType> Applied = new HashSet<PrestigeUpgradeType>();

    // ── Offer catalogue ───────────────────────────────────────────────────
    public readonly struct OfferData
    {
        public readonly string Title;
        public readonly string Description;
        public OfferData(string title, string desc) { Title = title; Description = desc; }
    }

    private static readonly Dictionary<PrestigeUpgradeType, OfferData> Catalogue
        = new Dictionary<PrestigeUpgradeType, OfferData>
    {
        { PrestigeUpgradeType.Overclock,  new OfferData("System Overclock",       "All ability cooldowns reduced by 20%. Stackable.") },
        { PrestigeUpgradeType.VitalSurge, new OfferData("Vital Surge",             "Permanently gain +2 max HP. Stackable.") },
        { PrestigeUpgradeType.Adrenaline, new OfferData("Adrenaline Protocol",     "Movement speed increased by 5%. Stackable up to +100%.") },
        { PrestigeUpgradeType.LastStand,  new OfferData("Last Stand",              "Once per mission: survive a lethal hit at 1 HP with a protective shield.") },
    };

    public static OfferData GetData(PrestigeUpgradeType type) => Catalogue[type];

    // ── Available types (not yet applied, or stackable) ───────────────────
    public static IEnumerable<PrestigeUpgradeType> GetAvailableTypes()
    {
        foreach (PrestigeUpgradeType t in System.Enum.GetValues(typeof(PrestigeUpgradeType)))
        {
            // LastStand can only be applied once
            if (t == PrestigeUpgradeType.LastStand && Applied.Contains(t)) continue;
            yield return t;
        }
    }

    // ── Apply ─────────────────────────────────────────────────────────────
    public static void Apply(PrestigeUpgradeType type)
    {
        Applied.Add(type);
        prestigeStackCount++;
        switch (type)
        {
            case PrestigeUpgradeType.Overclock:
                CooldownMultiplier *= 0.80f;
                break;

            case PrestigeUpgradeType.VitalSurge:
                PlayerHealth.Instance?.AddMaxHP(2);
                break;

            case PrestigeUpgradeType.Adrenaline:
                // 5% per stack, capped at 2x total (100% bonus)
                SpeedMultiplier = Mathf.Min(2f, SpeedMultiplier + 0.05f);
                break;

            case PrestigeUpgradeType.LastStand:
                HasLastStand = true;
                break;
        }
    }

    // Called when Last Stand triggers
    public static void UseLastStand() => LastStandUsed = true;

    // ── Reset at start of each run ────────────────────────────────────────
    public static void Reset()
    {
        CooldownMultiplier = 1f;
        SpeedMultiplier    = 1f;
        HasLastStand       = false;
        LastStandUsed      = false;
        prestigeStackCount = 0;
        Applied.Clear();
    }
}
