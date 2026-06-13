using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    protected AbilityDefinition definition;
    protected float lastUsedTime = -999f;

    public AbilityDefinition Definition => definition;
    public bool IsReady => Time.time >= lastUsedTime + (definition != null ? definition.Cooldown : 5f);
    public float CooldownProgress => definition != null
        ? Mathf.Clamp01((Time.time - lastUsedTime) / definition.Cooldown)
        : 1f;

    public virtual void Initialise(AbilityDefinition def)
    {
        definition = def;
    }

    public virtual void Upgrade(AbilityDefinition newDef)
    {
        definition = newDef;
        OnUpgraded();
    }

    public void TryActivate()
    {
        if (!IsReady)
            return;

        lastUsedTime = Time.time;
        Activate();
    }

    protected abstract void Activate();
    protected virtual void OnUpgraded() { }

    public virtual string GetAbilityName() => definition != null ? definition.AbilityName : "Unknown";
    public virtual int GetStarLevel() => definition != null ? definition.StarLevel : 0;
}