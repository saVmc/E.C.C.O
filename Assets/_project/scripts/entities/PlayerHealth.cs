using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int healthPerLevel = 2;
    [SerializeField] private float iFrameDuration = 1.2f;

    [Header("Damage Flash (sprite)")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float damageFlashDuration = 0.12f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvincible { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action<int>      OnDamaged;        // passes damage amount for screen fx scaling
    public event Action           OnDeath;

    private SpriteRenderer sr;
    private Color originalColor;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        // Safety clamp — Inspector value may still be old 100
        if (maxHealth > 20) maxHealth = 10;
        CurrentHealth = maxHealth;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    private void Start()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        maxHealth += healthPerLevel;
        CurrentHealth = Mathf.Min(CurrentHealth + healthPerLevel, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || IsInvincible || amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnDamaged?.Invoke(amount);

        if (sr != null) StartCoroutine(DamageFlashRoutine());

        if (CurrentHealth <= 0)
            Die();
        else
            StartCoroutine(IFrameRoutine());
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        IsInvincible = true;

        // ScreenFX listens here to start the red-flash death sequence
        OnDeath?.Invoke();

        // TimeParadoxDeathController handles: animator Die trigger, disabling movement/shooting, explosion VFX
        TimeParadoxDeathController dc = GetComponent<TimeParadoxDeathController>();
        if (dc != null) dc.ForceDeath();
    }

    private IEnumerator IFrameRoutine()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(iFrameDuration);
        IsInvincible = false;
    }

    private IEnumerator DamageFlashRoutine()
    {
        sr.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        sr.color = originalColor;
    }
}
