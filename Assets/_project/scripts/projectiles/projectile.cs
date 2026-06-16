using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField] private GameObject explosionVFXPrefab;
    protected Vector2 direction;
    protected float speed;
    protected float lifetime;
    protected int damage;
    protected GameObject owner;
    protected LayerMask hitMask;
    private int pierceCount = 0;
    private bool slowsEnemies = false;
    private float slowMultiplier = 0.4f;
    private float slowDuration = 2f;
    private TrailRenderer trail;
    private int maxPierceCount = 0;
    private float destroyTime;
    private bool rotateToDirection;
    private bool isPiercing = false;
    private Rigidbody2D rb;
    private bool isExplosive = false;
    private float explosionRadius = 2f;
    private bool isExecutioner = false;
    private float executionThreshold = 0.2f;
    private bool isRicochet = false;
    private int ricochetCount = 1;
    private int currentRicochets = 0;
    private bool marksEnemies = false;
    private float markDamageMultiplier = 2f;
    private float markDuration = 8f;
    private bool isChainKill = false;
    private HashSet<GameObject> killedEnemies = new HashSet<GameObject>();
    private bool shockwaveOnKill = false;
    private float shockwaveRadius = 3f;
    private float shockwaveDamage = 15f;
    private bool shockwaveMarks = false;
    private System.Action ammoOnKillCallback = null;
    private bool knockbackOnHit = false;
    private float knockbackForce = 12f;
    private bool burnsEnemies = false;
    private int burnDamage = 4;
    private int burnTicks = 3;
    private float burnTickInterval = 1f;
    private bool burnWildfire = false;
    private float burnWildfireRadius = 2.5f;
    private bool burnNapalm = false;
    private float burnNapalmRadius = 2.5f;
    private int burnNapalmDamage = 20;

    public void SetKnockback(float force) { knockbackOnHit = true; knockbackForce = force; }

    public void SetBurn(int damage, int ticks, float interval, bool wildfire = false, float wildfireRadius = 2.5f, bool napalm = false, float napalmRadius = 2.5f, int napalmDamage = 20)
    {
        burnsEnemies = true; burnDamage = damage; burnTicks = ticks; burnTickInterval = interval;
        burnWildfire = wildfire; burnWildfireRadius = wildfireRadius;
        burnNapalm = napalm; burnNapalmRadius = napalmRadius; burnNapalmDamage = napalmDamage;
    }

    public void SetScaleUp(float startFraction, float duration)
    {
        StartCoroutine(ScaleUpRoutine(startFraction, duration));
    }

    private IEnumerator ScaleUpRoutine(float startFraction, float duration)
    {
        Vector3 fullScale = transform.localScale;
        transform.localScale = fullScale * startFraction;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(fullScale * startFraction, fullScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = fullScale;
    }

    public void SetExplosive(float radius) { isExplosive = true; explosionRadius = radius; }
    public void SetSlow(float multiplier, float duration) { slowsEnemies = true; slowMultiplier = multiplier; slowDuration = duration; }
    public void SetExecutioner(float threshold) { isExecutioner = true; executionThreshold = threshold; }
    public void SetRicochet(int count) { isRicochet = true; ricochetCount = count; }
    public void SetPiercing(bool value) => isPiercing = value;
    public void SetPiercing(int maxPierces) { isPiercing = true; maxPierceCount = maxPierces; }
    public void SetMark(float multiplier, float duration) { marksEnemies = true; markDamageMultiplier = multiplier; markDuration = duration; }
    public void SetTint(Color color) { SpriteRenderer sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.color = color; }
    public void SetChainKill() { isChainKill = true; }
    public void SetShockwaveOnKill(float radius, float damage, bool marks) { shockwaveOnKill = true; shockwaveRadius = radius; shockwaveDamage = damage; shockwaveMarks = marks; }
    public void SetAmmoOnKillCallback(System.Action callback) { ammoOnKillCallback = callback; }

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

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.enabled = false;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void EnableTrail(Color color)
    {
        if (trail != null)
        {
            trail.enabled = true;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }
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
        if (Time.time >= destroyTime)
            Destroy(gameObject);
    }

    protected virtual void FixedUpdate()
    {
        if (rb != null)
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        else
            transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            return;

        if (other.GetComponentInParent<Projectile>() != null)
            return;

        if (owner != null && (other.transform.root.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        // Chain kill: skip enemies we've already killed with this bullet
        if (killedEnemies.Contains(other.transform.root.gameObject))
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
            Enemy ricoEnemy = other.GetComponentInParent<Enemy>();
            OnHit(other);
            ApplyOnHitEffects(other);
            bool ricKilled = ricoEnemy != null && ricoEnemy.IsDead;
            if (ricKilled) { ammoOnKillCallback?.Invoke(); if (shockwaveOnKill) DoShockwave(); }
            currentRicochets++;
            if (currentRicochets >= ricochetCount) { Destroy(gameObject); return; }
            GameObject justHit = other.transform.root.gameObject;
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Enemy nearest = null;
            float nearestDist = float.MaxValue;
            foreach (Enemy e in allEnemies)
            {
                if (e.gameObject == justHit) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = e; }
            }
            if (nearest != null)
            {
                direction = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
                transform.right = direction;
            }
            return;
        }

        Enemy hitEnemy = other.GetComponentInParent<Enemy>();
        OnHit(other);
        ApplyOnHitEffects(other);

        bool justKilled = hitEnemy != null && hitEnemy.IsDead;
        if (justKilled)
        {
            ammoOnKillCallback?.Invoke();
            if (shockwaveOnKill) DoShockwave();
            if (isChainKill)
            {
                killedEnemies.Add(other.transform.root.gameObject);
                return;
            }
        }

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

    private void ApplyOnHitEffects(Collider2D other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;
        if (slowsEnemies)  enemy.ApplySlow(slowMultiplier, slowDuration);
        if (marksEnemies)  enemy.ApplyMark(markDamageMultiplier, markDuration);
        if (burnsEnemies)  enemy.ApplyBurn(burnDamage, burnTicks, burnTickInterval, burnWildfire, burnWildfireRadius, burnNapalm, burnNapalmRadius, burnNapalmDamage);
        if (knockbackOnHit)
        {
            Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.Knockback(dir, knockbackForce);
        }
    }

    private void DoShockwave()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player")) continue;
            Enemy e = hit.GetComponentInParent<Enemy>();
            if (e != null && !e.IsDead)
            {
                e.TakeDamage((int)shockwaveDamage);
                if (shockwaveMarks) e.ApplyMark(markDamageMultiplier, markDuration);
            }
        }
    }

    private void Explode()
    {
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                continue;
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
        }
    }

    protected abstract void OnHit(Collider2D other);
}
