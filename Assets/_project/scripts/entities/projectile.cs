using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool rotateToDirection = false;

    protected Vector2 direction;
    protected float speed;
    protected float lifetime;
    protected int damage;
    protected GameObject owner;

    private float destroyTime;

    public virtual void Initialize(Vector2 moveDirection, float moveSpeed, float lifeSeconds, int projectileDamage, GameObject projectileOwner)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
        lifetime = lifeSeconds;
        damage = projectileDamage;
        owner = projectileOwner;
        destroyTime = Time.time + lifetime;

        if (rotateToDirection && direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner)
        {
            return;
        }

        if (((1 << other.gameObject.layer) & hitMask) == 0)
        {
            return;
        }

        OnHit(other);
        Destroy(gameObject);
    }

    protected abstract void OnHit(Collider2D other);
}