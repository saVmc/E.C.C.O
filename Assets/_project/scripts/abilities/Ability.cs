using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    protected AbilityDefinition definition;
    private AbilityDefinition baseDefinition;
    protected float lastUsedTime = -999f;

    public AbilityDefinition Definition => definition;
    public AbilityDefinition BaseDefinition => baseDefinition;
    public virtual bool IsReady => Time.time >= lastUsedTime + (definition != null ? definition.Cooldown : 5f);
    public virtual float CooldownProgress => definition != null
        ? Mathf.Clamp01((Time.time - lastUsedTime) / definition.Cooldown)
        : 1f;

    public virtual void Initialise(AbilityDefinition def)
{
    definition = def;
    baseDefinition = def;
    OnUpgraded();
}

    public virtual void Upgrade(AbilityDefinition newDef)
    {
        definition = newDef;
        OnUpgraded();
    }

    public virtual void TryActivate()
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