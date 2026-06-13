using UnityEngine;

[CreateAssetMenu(menuName = "E.C.C.O/Experience/Exp Orb Profile", fileName = "ExpOrbProfile")]
public sealed class ExpOrbProfile : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private int expValue = 5;
    [SerializeField] private float attractRadius = 4f;
    [SerializeField] private float flySpeed = 8f;

    [Header("Visuals")]
    [SerializeField] private Color orbColor = Color.cyan;
    [SerializeField] private float orbScale = 0.3f;

    public int ExpValue => expValue;
    public float AttractRadius => attractRadius;
    public float FlySpeed => flySpeed;
    public Color OrbColor => orbColor;
    public float OrbScale => Mathf.Max(0.01f, orbScale);
}