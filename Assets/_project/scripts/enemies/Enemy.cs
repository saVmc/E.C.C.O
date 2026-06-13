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

    public event System.Action<int> OnDeath;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

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

        MoveTowardPlayer();
    }

    protected virtual void MoveTowardPlayer()
    {
        if (profile == null)
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = direction * profile.MoveSpeed;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead)
            return;

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
        spriteRenderer.color = Color.Lerp(Color.white, Color.red, t);
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        OnDeath?.Invoke(profile != null ? profile.CalculateExpDrop() : 0);
    
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

    public float CurrentHealth => currentHealth;
    public float MaxHealth => profile != null ? profile.MaxHealth : 1f;
    public bool IsDead => isDead;
}