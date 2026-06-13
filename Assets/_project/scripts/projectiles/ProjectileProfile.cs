using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Weapons/Projectile Profile", fileName = "ProjectileProfile")]
public sealed class ProjectileProfile : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxRange = 25f;

    [Header("Visuals")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private float scale = 1f;

    [Header("Physics")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool rotateToDirection = false;

    public int Damage => damage;
    public float Speed => speed;
    public float MaxRange => maxRange;
    public Sprite Sprite => sprite;
    public Color TintColor => tintColor;
    public float Scale => Mathf.Max(0.01f, scale);
    public LayerMask HitMask => hitMask;
    public bool RotateToDirection => rotateToDirection;

    public float Lifetime => speed > 0.0001f ? maxRange / speed : 2f;

    public ProjectileProfile RuntimeCopy()
    {
        return Instantiate(this);
    }
}