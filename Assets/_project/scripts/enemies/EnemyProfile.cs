using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Enemies/Enemy Profile", fileName = "EnemyProfile")]
public sealed class EnemyProfile : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int contactDamage = 1;

    [Header("Visuals")]
    [SerializeField] private float poofScalePeak = 1.6f;
    [SerializeField] private float poofDuration = 0.18f;

    [Header("EXP")]
    [SerializeField] private int baseExpValue = 5;
    [SerializeField] private ExpOrbProfile expOrbProfile;
    public ExpOrbProfile ExpOrbProfile => expOrbProfile;

    private const float referenceHealth = 25f;

    public int CalculateExpDrop() => Mathf.Max(1, Mathf.RoundToInt(baseExpValue * Mathf.Max(1f, maxHealth / referenceHealth)));
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int ContactDamage => contactDamage;
    public float PoofScalePeak => poofScalePeak;
    public float PoofDuration => poofDuration;
}