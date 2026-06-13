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

    private float destroyTime;
    private bool rotateToDirection;

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

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && (other.transform.root.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        OnHit(other);
        Destroy(gameObject);
    }

    protected abstract void OnHit(Collider2D other);
}