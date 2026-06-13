using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Gun Upgrade", fileName = "GunUpgrade")]
public sealed class GunUpgrade : ScriptableObject
{
    [SerializeField] private string displayName = "Upgrade";
    [SerializeField] private float fireCooldownMultiplier = 0.9f;
    [SerializeField] private float projectileSpeedMultiplier = 1f;
    [SerializeField] private float projectileLifetimeMultiplier = 1f;
    [SerializeField] private int projectileDamageBonus = 0;
    [SerializeField] private int magazineSizeBonus = 0;
    [SerializeField] private float reloadTimeMultiplier = 1f;
    [SerializeField] private Vector2 firePointOffsetDelta = Vector2.zero;
    [SerializeField] private Vector2 aimPivotOffsetDelta = Vector2.zero;

    public string DisplayName => displayName;
    public float FireCooldownMultiplier => fireCooldownMultiplier;
    public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
    public float ProjectileLifetimeMultiplier => projectileLifetimeMultiplier;
    public int ProjectileDamageBonus => projectileDamageBonus;
    public int MagazineSizeBonus => magazineSizeBonus;
    public float ReloadTimeMultiplier => reloadTimeMultiplier;
    public Vector2 FirePointOffsetDelta => firePointOffsetDelta;
    public Vector2 AimPivotOffsetDelta => aimPivotOffsetDelta;
}