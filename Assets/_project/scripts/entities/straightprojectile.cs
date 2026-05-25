using UnityEngine;

public sealed class StraightProjectile : Projectile
{
    protected override void OnHit(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}