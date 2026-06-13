using System.Collections;
using UnityEngine;

public sealed class FireTrail : MonoBehaviour
{
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0f, 1f);

    private int damage = 1;
    private float tickInterval = 1f;
    private float lifetime = 8f;

    public void SetDamage(int dmg) => damage = dmg;

    private void Start()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startColor = fireColor;
            fireParticles.Play();
        }

        StartCoroutine(DamageTick());
    }

    private IEnumerator DamageTick()
    {
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
            foreach (Collider2D hit in hits)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damage);
            }
        }
    }
}