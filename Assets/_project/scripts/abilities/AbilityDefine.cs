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

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject vfxPrefabA;
    [SerializeField] private GameObject vfxPrefabB;
    [SerializeField] private GameObject vfxPrefabC;
    [Tooltip("Sprite shown in-world during the ability (e.g. the chupa lance).")]
    [SerializeField] private Sprite visualSprite;
    [Tooltip("How wide the visual sprite should appear in world units. Adjust until it looks right in-game.")]
    [SerializeField] private float  visualSpriteWidth = 2f;

    public GameObject VfxPrefabA       => vfxPrefabA;
    public GameObject VfxPrefabB       => vfxPrefabB;
    public GameObject VfxPrefabC       => vfxPrefabC;
    public Sprite     VisualSprite     => visualSprite;
    public float      VisualSpriteWidth => visualSpriteWidth;

    public string AbilityName => abilityName;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int StarLevel => starLevel;
    public float Cooldown => cooldown;
    public AbilityDefinition NextStarDefinition => nextStarDefinition;
    public bool IsMaxStar => nextStarDefinition == null;
}