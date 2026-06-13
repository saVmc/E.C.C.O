using System.Collections;
using UnityEngine;

public sealed class DashAbility : Ability
{
    [SerializeField] private float dashDistance = 4f;
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

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        playerMovement = GetComponentInParent<PlayerMovement>();
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

    if (phaseThrough)
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

    while (elapsed < dashDuration)
    {
        elapsed += Time.deltaTime;
        rb.linearVelocity = direction * speed;

        if (leaveFireTrail && fireTrailPrefab != null)
            Instantiate(fireTrailPrefab, transform.position, Quaternion.identity);

        yield return null;
    }

    rb.linearVelocity = Vector2.zero;

    if (phaseThrough)
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), false);

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

        switch (definition.StarLevel)
        {
            case 1: dashDistance = 5f; break;
            case 2: dashDistance = 6f; speedBoostMultiplier = 2f; break;
            case 3: phaseThrough = true; break;
            case 4: leaveFireTrail = true; break;
            case 5: dashDistance = 8f; speedBoostDuration = 2f; phaseThrough = true; leaveFireTrail = true; break;
        }
    }
}