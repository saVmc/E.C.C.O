using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Abilities/Ability Definition", fileName = "AbilityDefinition")]
public sealed class AbilityDefinition : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string abilityName = "Dash";
    [SerializeField] private string displayName = "Momentum";
    [SerializeField] private string description = "Does something cool.";
    [SerializeField] private Sprite icon;
    [SerializeField] private int starLevel = 0;

    [Header("Stats")]
    [SerializeField] private float cooldown = 5f;

    [Header("Upgrade Chain")]
    [SerializeField] private AbilityDefinition nextStarDefinition;

    public string AbilityName => abilityName;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int StarLevel => starLevel;
    public float Cooldown => cooldown;
    public AbilityDefinition NextStarDefinition => nextStarDefinition;
    public bool IsMaxStar => nextStarDefinition == null;
}