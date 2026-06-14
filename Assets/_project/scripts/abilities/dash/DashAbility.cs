using System.Collections;
using UnityEngine;

public sealed class DashAbility : Ability

{
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private int fireTrailDamage = 1;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 1f;
    [SerializeField] private bool phaseThrough = false;
    [SerializeField] private bool leaveFireTrail = false;
    [SerializeField] private GameObject fireTrailPrefab;
    [SerializeField] private GameObject speedBoostParticlePrefab;
    [SerializeField] private float speedBoostParticleInterval = 0.05f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private SpriteRenderer playerSprite;
private Material playerMaterial;

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerSprite = GetComponentInParent<SpriteRenderer>();
    }

    protected override void Activate()
    {
        Vector2 dashDir = playerMovement != null
            ? playerMovement.GetMovementDirection()
            : Vector2.right;

        if (dashDir.sqrMagnitude < 0.0001f)
            dashDir = Vector2.right;

        StartCoroutine(DashRoutine(dashDir.normalized));
    }

    private IEnumerator DashRoutine(Vector2 direction)
{
    float elapsed = 0f;
    float speed = dashDistance / dashDuration;

    PlayerMovement movement = GetComponentInParent<PlayerMovement>();
    if (movement != null) movement.enabled = false;

    int enemyLayer = LayerMask.NameToLayer("Enemy");

    if (phaseThrough && enemyLayer >= 0)
    {
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);
        if (playerSprite != null)
        {
            Color c = playerSprite.color;
            c.a = 0.5f;
            playerSprite.color = new Color(0.3f, 0.6f, 1f, 0.5f);
        }
    }

    // Star 5 outline
    bool hasOutline = definition != null && definition.StarLevel >= 5;
    if (hasOutline && playerSprite != null)
        playerSprite.material.SetFloat("_Outline", 1f);

    while (elapsed < dashDuration)
    {
        elapsed += Time.deltaTime;
        rb.linearVelocity = direction * speed;

        if (leaveFireTrail && fireTrailPrefab != null)
        {
            GameObject trail = Instantiate(fireTrailPrefab, transform.position, Quaternion.identity);
            FireTrail ft = trail.GetComponent<FireTrail>();
            if (ft != null) ft.SetDamage(fireTrailDamage);
            Destroy(trail, 8f);
        }

        yield return null;
    }

    rb.linearVelocity = Vector2.zero;

    if (phaseThrough && enemyLayer >= 0)
    {
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false);
        if (playerSprite != null)
            playerSprite.color = Color.white;
    }

    if (hasOutline && playerSprite != null)
        playerSprite.material.SetFloat("_Outline", 0f);

    if (movement != null) movement.enabled = true;

    if (speedBoostMultiplier > 1f)
        StartCoroutine(SpeedBoostRoutine());
}
    private IEnumerator SpeedBoostRoutine()
{
    PlayerMovement movement = GetComponentInParent<PlayerMovement>();
    if (movement != null)
        movement.ApplySpeedBoost(speedBoostMultiplier, speedBoostDuration);

    float elapsed = 0f;
    float particleTimer = 0f;

    while (elapsed < speedBoostDuration)
    {
        elapsed += Time.deltaTime;
        particleTimer += Time.deltaTime;

        if (speedBoostParticlePrefab != null && particleTimer >= speedBoostParticleInterval)
        {
            particleTimer = 0f;
            GameObject p = Instantiate(speedBoostParticlePrefab, transform.position, Quaternion.identity);
            Destroy(p, 1f);
        }

        yield return null;
    }
}

    protected override void OnUpgraded()
    {
        if (definition == null) return;

        if (definition.VfxPrefabA != null) fireTrailPrefab = definition.VfxPrefabA;
        if (definition.VfxPrefabB != null) speedBoostParticlePrefab = definition.VfxPrefabB;

        switch (definition.StarLevel)
        {
                case 0:
    dashDistance = 4f;
    speedBoostMultiplier = 1f;
    phaseThrough = false;
    leaveFireTrail = false;
    break;
            case 1: dashDistance = 5f; break;
            case 2: dashDistance = 6f; speedBoostMultiplier = 2f; break;
            case 3: phaseThrough = true; break;
            case 4: leaveFireTrail = true; break;
            case 5: dashDistance = 8f; speedBoostDuration = 2f; phaseThrough = true; leaveFireTrail = true; break;
        }
    }
}