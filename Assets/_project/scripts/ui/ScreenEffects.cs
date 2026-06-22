using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private string afterDeathSceneName = "DifficultySelect";

    private static readonly Color RedColor  = new Color(0.65f, 0f,    0f);
    private static readonly Color CyanColor = new Color(0f,    0.75f, 0.9f);

    private Image overlay;
    private PlayerHealth ph;
    private float flashAlpha      = 0f;
    private bool  deathRunning    = false;
    private bool  isRecording     = false;
    private float recordingAlpha  = 0f;

    public static ScreenFX Instance { get; private set; }

    // Fired after the red→black animation completes, with timeScale already 0
    public static event System.Action OnDeathScreenReady;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = Color.clear;

        // Force the overlay RectTransform to fill the entire parent canvas
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
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

    // ── Public API ─────────────────────────────────────────────────────────────

    public void SetRecording(bool active) => isRecording = active;

    public void TriggerAmmoFlash() { } // no-op — ammo overlay removed

    // ── Update ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (deathRunning) return;

        flashAlpha = Mathf.Max(0f, flashAlpha - flashDecaySpeed * Time.deltaTime);

        float recTarget = 0f;
        if (isRecording)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
            recTarget = Mathf.Lerp(0.10f, 0.22f, pulse);
        }
        recordingAlpha = Mathf.MoveTowards(recordingAlpha, recTarget, 3f * Time.unscaledDeltaTime);

        float hpRatio = ph != null ? (float)ph.CurrentHealth / ph.MaxHealth : 1f;
        float pulseAlpha = 0f;
        if (ph != null && hpRatio < lowHpThreshold && !ph.IsDead)
        {
            float severity = 1f - hpRatio / lowHpThreshold;
            float pulse = (Mathf.Sin(Time.time * lowHpPulseSpeed * Mathf.PI) + 1f) * 0.5f;
            pulseAlpha = pulse * lowHpMaxAlpha * severity;
        }
        float redAlpha = Mathf.Clamp01(flashAlpha + pulseAlpha);

        Color finalColor;
        if (redAlpha > 0.01f)
            finalColor = new Color(RedColor.r, RedColor.g, RedColor.b, redAlpha);
        else if (recordingAlpha > 0.01f)
            finalColor = new Color(CyanColor.r, CyanColor.g, CyanColor.b, recordingAlpha);
        else
            finalColor = Color.clear;

        overlay.color = finalColor;
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

        // Notify any listening death screen
        if (OnDeathScreenReady != null)
        {
            overlay.color = Color.clear; // Death screen has its own black BG; clear ours
            OnDeathScreenReady.Invoke();
            yield break; // Death screen handles navigation
        }

        // Fallback when no death screen is in the scene
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(true);
            yield break;
        }

        yield return new WaitForSecondsRealtime(2.2f);
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(afterDeathSceneName))
            SceneManager.LoadScene(afterDeathSceneName);
    }
}
