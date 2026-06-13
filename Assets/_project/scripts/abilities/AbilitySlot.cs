using UnityEngine;

public sealed class AbilitySlot
{
    public Ability Ability { get; private set; }
    public KeyCode HotKey { get; private set; }
    public bool IsEmpty => Ability == null;

    public AbilitySlot(KeyCode hotkey)
    {
        HotKey = hotkey;
    }

    public void AssignAbility(Ability ability)
    {
        Ability = ability;
    }

    public void Clear()
    {
        Ability = null;
    }
}