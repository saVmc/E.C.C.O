using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public static event Action OnLastStandActivated;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    private int healthPerLevel = 1;
    [SerializeField] private float iFrameDuration = 1.2f;


    [Header("Damage Flash (sprite)")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float damageFlashDuration = 0.12f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

private bool forcedInvincible = false;
    private bool iframeInvincible = false;
    public  bool IsInvincible => forcedInvincible || iframeInvincible;
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action<int>      OnDamaged;
    public event Action           OnDeath;

    private SpriteRenderer sr;
    private Color originalColor;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

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

    public void SetInvincible(bool state) => forcedInvincible = state;

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void AddMaxHP(int amount)
    {
        if (amount <= 0) return;
        maxHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        // Last Stand: survive once at 1 HP with red border shield
        if (PrestigeEffects.HasLastStand && !PrestigeEffects.LastStandUsed)
        {
            PrestigeEffects.UseLastStand();
            CurrentHealth = 1;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            StartCoroutine(IFrameRoutine());
            StartCoroutine(LastStandShieldRoutine());
            OnLastStandActivated?.Invoke();
            return;
        }

        IsDead = true;
        forcedInvincible = true;

OnDeath?.Invoke();

TimeParadoxDeathController dc = GetComponent<TimeParadoxDeathController>();
        if (dc != null) dc.ForceDeath();
    }

    private IEnumerator IFrameRoutine()
    {
        iframeInvincible = true;
        yield return new WaitForSeconds(iFrameDuration);
        iframeInvincible = false;
    }

    private IEnumerator DamageFlashRoutine()
    {
        sr.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        sr.color = originalColor;
    }

    private IEnumerator LastStandShieldRoutine()
    {
        const int   ringPoints = 48;
        const float radius     = 1.2f;

        GameObject ringGO = new GameObject("_LastStandRing");
        ringGO.transform.SetParent(transform);
        ringGO.transform.localPosition = Vector3.zero;

        LineRenderer lr = ringGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = ringPoints;
        lr.startWidth    = 0.12f;
        lr.endWidth      = 0.12f;
        lr.sortingOrder  = 25;

        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        Material mat = sh != null ? new Material(sh) : null;
        if (mat != null) lr.material = mat;

        for (int i = 0; i < ringPoints; i++)
        {
            float a = (i / (float)ringPoints) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }

        // Pulse red for the iFrame duration
        float elapsed = 0f;
        while (elapsed < iFrameDuration)
        {
            elapsed += Time.deltaTime;
            if (lr == null) yield break;
            float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 10f);
            float alpha = 0.6f + 0.4f * pulse;
            lr.startColor = new Color(1f, 0.05f, 0.05f, alpha);
            lr.endColor   = new Color(1f, 0.05f, 0.05f, alpha * 0.3f);
            yield return null;
        }

        // Fade out
        float fadeElapsed = 0f;
        const float fadeDur = 0.4f;
        while (fadeElapsed < fadeDur)
        {
            fadeElapsed += Time.deltaTime;
            if (lr == null) yield break;
            float a = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDur);
            lr.startColor = new Color(1f, 0.05f, 0.05f, a);
            lr.endColor   = new Color(1f, 0.05f, 0.05f, a * 0.3f);
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (ringGO != null) Destroy(ringGO);
    }
}