using UnityEngine;

public sealed class GunUpgradeOffer
{
    public GunUpgrade Upgrade { get; private set; }
    public string DisplayName { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; }
    public int StarLevel { get; private set; }

    public string GunName { get; private set; }
public string UpgradeName { get; private set; }

public GunUpgradeOffer(GunUpgrade upgrade, string gunName)
{
    Upgrade = upgrade;
    GunName = gunName;
    UpgradeName = upgrade.DisplayName;
    DisplayName = $"{gunName} — {upgrade.DisplayName}";
    Description = upgrade.Description;
    Icon = upgrade.Icon;
    StarLevel = upgrade.StarLevel;
}


    
    public void Apply(Gun gun)
    {
        if (gun != null && Upgrade != null)
            gun.ApplyUpgrade(Upgrade);
    }
}