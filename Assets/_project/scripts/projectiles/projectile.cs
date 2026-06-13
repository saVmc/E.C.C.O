using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class Projectile : MonoBehaviour
{
    protected Vector2 direction;
    protected float speed;
    protected float lifetime;
    protected int damage;
    protected GameObject owner;
    protected LayerMask hitMask;
    private int pierceCount = 0;
private int maxPierceCount = 0;
    private float destroyTime;
    private bool rotateToDirection;
    private bool isPiercing = false;
    private bool isExplosive = false;
private float explosionRadius = 2f;
private bool isExecutioner = false;
private float executionThreshold = 0.2f;
private bool isRicochet = false;
private int ricochetCount = 1;
private int currentRicochets = 0;

public void SetExplosive(float radius) { isExplosive = true; explosionRadius = radius; }
public void SetExecutioner(float threshold) { isExecutioner = true; executionThreshold = threshold; }
public void SetRicochet(int count) { isRicochet = true; ricochetCount = count; }
    public void SetPiercing(bool value) => isPiercing = value;

    public virtual void Initialize(Vector2 moveDirection, float moveSpeed, float lifeSeconds, int projectileDamage, GameObject projectileOwner, LayerMask mask, bool rotate)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
        lifetime = lifeSeconds;
        damage = projectileDamage;
        owner = projectileOwner;
        hitMask = mask;
        rotateToDirection = rotate;
        destroyTime = Time.time + lifetime;
    }

    public virtual void ApplyProfile(ProjectileProfile profile)
    {
        if (profile == null)
            return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (profile.Sprite != null)
                sr.sprite = profile.Sprite;
            sr.color = profile.TintColor;
        }

        Light2D light2D = GetComponentInChildren<Light2D>();
        if (light2D != null)
            light2D.color = profile.TintColor;

        transform.localScale = Vector3.one * profile.Scale;
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= destroyTime)
            Destroy(gameObject);
    }

    public void SetPiercing(int maxPierces)
{
    isPiercing = true;
    maxPierceCount = maxPierces;
}
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && (other.transform.root.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;
        
        if (isExecutioner)
{
    Enemy enemy = other.GetComponentInParent<Enemy>();
    if (enemy != null && enemy.CurrentHealth / enemy.MaxHealth <= executionThreshold)
    {
        enemy.ExecutionKill();
        Destroy(gameObject);
        return;
    }
}

if (isExplosive)
{
    Explode();
    Destroy(gameObject);
    return;
}

if (isRicochet)
{
    OnHit(other);
    currentRicochets++;
    if (currentRicochets >= ricochetCount)
        Destroy(gameObject);
    // direction change handled below
    return;
}

        OnHit(other);
if (!isPiercing)
{
    Destroy(gameObject);
}
else
{
    pierceCount++;
    if (pierceCount >= maxPierceCount)
        Destroy(gameObject);
}
    }

    private void Explode()
{
    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
    foreach (Collider2D hit in hits)
    {
        IDamageable damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);
    }
}
    protected abstract void OnHit(Collider2D other);
}