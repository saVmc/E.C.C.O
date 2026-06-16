using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class ScreenFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject deathScreenUI;

    [Header("Damage Flash")]
    [SerializeField] private float maxFlashAlpha = 0.55f;
    [SerializeField] private float flashDecaySpeed = 5f;

    [Header("Low HP Pulse")]
    [SerializeField] private float lowHpThreshold = 0.35f;
    [SerializeField] private float lowHpPulseSpeed = 1.8f;
    [SerializeField] private float lowHpMaxAlpha = 0.28f;

    [Header("Death Sequence")]
    [SerializeField] private float deathFadeToRedDuration = 0.7f;
    [SerializeField] private float deathRedHoldDuration = 0.5f;
    [SerializeField] private float deathFadeToBlackDuration = 1.4f;
    [SerializeField] private float deathScreenDelay = 0.6f;

    private static readonly Color RedColor = new Color(0.65f, 0f, 0f);

    private Image overlay;
    private PlayerHealth ph;
    private float flashAlpha = 0f;
    private bool deathRunning = false;

    private void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = Color.clear;
    }

    private void Start()
    {
        ph = PlayerHealth.Instance != null ? PlayerHealth.Instance : FindAnyObjectByType<PlayerHealth>();
        if (ph == null) return;

        ph.OnDamaged += HandleDamage;
        ph.OnDeath += HandleDeath;

        if (deathScreenUI != null)
            deathScreenUI.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ph == null) return;
        ph.OnDamaged -= HandleDamage;
        ph.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (deathRunning || ph == null) return;

        float hpRatio = (float)ph.CurrentHealth / ph.MaxHealth;

        flashAlpha = Mathf.Max(0f, flashAlpha - flashDecaySpeed * Time.deltaTime);

        float pulseAlpha = 0f;
        if (hpRatio < lowHpThreshold && !ph.IsDead)
        {
            float severity = 1f - hpRatio / lowHpThreshold;
            float pulse = (Mathf.Sin(Time.time * lowHpPulseSpeed * Mathf.PI) + 1f) * 0.5f;
            pulseAlpha = pulse * lowHpMaxAlpha * severity;
        }

        float totalAlpha = Mathf.Clamp01(flashAlpha + pulseAlpha);
        overlay.color = new Color(RedColor.r, RedColor.g, RedColor.b, totalAlpha);
    }

    private void HandleDamage(int amount)
    {
        if (ph == null) return;
        float fraction = (float)amount / ph.MaxHealth;
        flashAlpha = Mathf.Clamp01(flashAlpha + fraction * maxFlashAlpha * 3f);
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        deathRunning = true;

        Color startColor = overlay.color;
        Color targetRed = new Color(RedColor.r, RedColor.g, RedColor.b, 1f);

        float elapsed = 0f;
        while (elapsed < deathFadeToRedDuration)
        {
            elapsed += Time.deltaTime;
            overlay.color = Color.Lerp(startColor, targetRed, elapsed / deathFadeToRedDuration);
            yield return null;
        }
        overlay.color = targetRed;

        yield return new WaitForSeconds(deathRedHoldDuration);

        Color targetBlack = new Color(0f, 0f, 0f, 1f);
        elapsed = 0f;
        while (elapsed < deathFadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            overlay.color = Color.Lerp(targetRed, targetBlack, elapsed / deathFadeToBlackDuration);
            yield return null;
        }
        overlay.color = targetBlack;

        yield return new WaitForSeconds(deathScreenDelay);

        Time.timeScale = 0f;
        if (deathScreenUI != null)
            deathScreenUI.SetActive(true);
    }
}
