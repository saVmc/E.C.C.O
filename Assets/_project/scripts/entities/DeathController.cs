using UnityEngine;

public sealed class TimeParadoxDeathController : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private float explosionDelay = 0.45f;
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private float boomDelayJitter = 0.1f;
    [SerializeField] private ParticleSystem deathExplosionPrefab;

    private int currentHealth;
    private bool isDead;
    private bool isDying;
    private Animator animator;
    private Rigidbody2D body;
    private Collider2D[] colliders;
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;
    private GhostPlayer ghostPlayer;
    private RecordingVisualsManager visualsManager;
    private TimeParadoxDeathController partner;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        animator = GetComponentInChildren<Animator>(true);
        body = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
        ghostPlayer = GetComponent<GhostPlayer>();
        visualsManager = FindAnyObjectByType<RecordingVisualsManager>();
    }

    public void SetPartner(TimeParadoxDeathController other)
    {
        if (other == this)
            return;

        partner = other;

        if (isDead && partner != null && !partner.isDead)
        {
            partner.DieInternal(false);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isDying || amount <= 0)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            DieInternal(true);
        }
    }

    public void ForceDeath()
    {
        DieInternal(true);
    }

    private void DieInternal(bool triggerPartner)
    {
        if (isDead || isDying)
            return;

        isDead = true;

        if (triggerPartner && partner != null && !partner.isDead)
        {
            partner.DieInternal(false);
        }

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerShooter != null)
            playerShooter.enabled = false;

        if (ghostPlayer != null)
            ghostPlayer.enabled = false;

        if (body != null)
            body.simulated = false;

        if (colliders != null)
        {
            foreach (Collider2D collider2D in colliders)
            {
                if (collider2D != null)
                    collider2D.enabled = false;
            }
        }

        if (animator != null && !string.IsNullOrWhiteSpace(deathTriggerName))
        {
            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }

        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        float delayedExplosion = Mathf.Max(0f, explosionDelay + Random.Range(-boomDelayJitter, boomDelayJitter));
        yield return new WaitForSeconds(delayedExplosion);

        GameSfxManager.Instance?.PlayDeath(Random.Range(0.9f, 1.1f));
        SpawnDeathExplosion();

        float remainingDestroyDelay = Mathf.Max(0f, destroyDelay - delayedExplosion);
        yield return new WaitForSeconds(remainingDestroyDelay);

        isDead = true;
        Destroy(gameObject);
    }

    private void SpawnDeathExplosion()
    {
        if (visualsManager != null)
        {
            visualsManager.SpawnDeathExplosion(transform.position);
            return;
        }

        if (deathExplosionPrefab == null)
            return;

        ParticleSystem effect = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        effect.Play();
        Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }
}
