using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] protected EnemyProfile profile;
    [SerializeField] private ExpOrb expOrbPrefab;

    protected float currentHealth;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Transform player;

    private bool isDead = false;
    private float slowMultiplier = 1f;
    private float slowRemainingTime = 0f;
    private bool isStunned = false;
    private bool isMarked = false;
    private float markDamageMultiplier = 1f;
    private float markRemainingTime = 0f;
    private float scaledMaxHealth = 0f;
    private float scaledSpeedMultiplier = 1f;
    private float scaledExpMultiplier = 1f;
    public bool IsBoss { get; private set; }

    public event System.Action<int> OnDeath;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (profile != null)
        {
            currentHealth = profile.MaxHealth;
            if (profile.Sprite != null)
                spriteRenderer.sprite = profile.Sprite;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (isDead || player == null)
            return;

        if (slowRemainingTime > 0f)
        {
            slowRemainingTime -= Time.deltaTime;
            if (slowRemainingTime <= 0f)
            {
                slowMultiplier = 1f;
                isStunned = false;
                UpdateHealthVisual();
            }
        }

        if (markRemainingTime > 0f)
        {
            markRemainingTime -= Time.deltaTime;
            if (markRemainingTime <= 0f)
            {
                isMarked = false;
                markDamageMultiplier = 1f;
                UpdateHealthVisual();
            }
        }

        MoveTowardPlayer();
    }

    protected virtual void MoveTowardPlayer()
    {
        if (profile == null)
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = direction * profile.MoveSpeed * slowMultiplier * scaledSpeedMultiplier;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (isMarked) amount = Mathf.RoundToInt(amount * markDamageMultiplier);
        currentHealth -= amount;
        UpdateHealthVisual();

        if (currentHealth <= 0f)
            Die();
    }

    protected virtual void UpdateHealthVisual()
    {
        if (profile == null)
            return;

        float t = 1f - Mathf.Clamp01(currentHealth / profile.MaxHealth);
        Color healthColor = Color.Lerp(Color.white, Color.red, t);
        if (isMarked) healthColor = Color.Lerp(healthColor, new Color(1f, 0.92f, 0.1f), 0.55f);
        spriteRenderer.color = isStunned ? Color.Lerp(healthColor, new Color(0.3f, 0.6f, 1f), 0.6f) : healthColor;
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        int exp = Mathf.RoundToInt((profile != null ? profile.CalculateExpDrop() : 0) * scaledExpMultiplier);
        OnDeath?.Invoke(exp);
    
    if (expOrbPrefab != null && profile != null && profile.ExpOrbProfile != null)
    {
        ExpOrb orb = Instantiate(expOrbPrefab, transform.position, Quaternion.identity);
        orb.SetProfile(profile.ExpOrbProfile);
        orb.SetExpValue(profile.CalculateExpDrop());
    }
    StartCoroutine(PoofAndDestroy());
}

    private IEnumerator PoofAndDestroy()
    {
        float duration = profile != null ? profile.PoofDuration : 0.18f;
        float peak = profile != null ? profile.PoofScalePeak : 1.6f;

        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, peak, Mathf.Sin(t * Mathf.PI));
            transform.localScale = originalScale * scale;
            float alpha = Mathf.Lerp(1f, 0f, t * t);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // Hook into player health here when ready
            // e.g. collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(profile.ContactDamage);
        }
    }

    public void ApplyProfile(EnemyProfile newProfile)
    {
        profile = newProfile;
        currentHealth = profile.MaxHealth;
        if (profile.Sprite != null)
            spriteRenderer.sprite = profile.Sprite;
    }

    public void ApplyMark(float multiplier, float duration)
    {
        isMarked = true;
        markDamageMultiplier = Mathf.Max(markDamageMultiplier, multiplier);
        markRemainingTime = Mathf.Max(markRemainingTime, duration);
        UpdateHealthVisual();
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        slowRemainingTime = Mathf.Max(slowRemainingTime, duration);
        isStunned = slowMultiplier == 0f;
        UpdateHealthVisual();
    }

    public void PullToward(Vector2 target, float pullForce)
    {
        if (isDead) return;
        Vector2 direction = ((Vector2)target - rb.position).normalized;
        rb.linearVelocity = direction * pullForce;
    }

    public void ExecutionKill()
{
    if (isDead) return;
    isDead = true;
    rb.linearVelocity = Vector2.zero;
    OnDeath?.Invoke(profile != null ? profile.CalculateExpDrop() : 0);
    if (expOrbPrefab != null && profile != null && profile.ExpOrbProfile != null)
    {
        ExpOrb orb = Instantiate(expOrbPrefab, transform.position, Quaternion.identity);
        orb.SetProfile(profile.ExpOrbProfile);
        orb.SetExpValue(profile.CalculateExpDrop());
    }
    StartCoroutine(ExecutionPoofAndDestroy());
}

private IEnumerator ExecutionPoofAndDestroy()
{
    float duration = profile != null ? profile.PoofDuration : 0.18f;
    float peak = profile != null ? profile.PoofScalePeak : 1.6f;
    Vector3 originalScale = transform.localScale;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        float scale = Mathf.Lerp(1f, peak, Mathf.Sin(t * Mathf.PI));
        transform.localScale = originalScale * scale;
        float alpha = Mathf.Lerp(1f, 0f, t * t);
        spriteRenderer.color = new Color(1f, 0.9f, 0f, alpha);
        yield return null;
    }

    Destroy(gameObject);
}

    public void ScaleStats(float healthMult, float speedMult, float expMult, bool isBoss = false)
    {
        currentHealth *= healthMult;
        scaledMaxHealth = currentHealth;
        scaledSpeedMultiplier = speedMult;
        scaledExpMultiplier = expMult;
        IsBoss = isBoss;
        UpdateHealthVisual();
    }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => scaledMaxHealth > 0f ? scaledMaxHealth : (profile != null ? profile.MaxHealth : 1f);
    public bool IsDead => isDead;
}