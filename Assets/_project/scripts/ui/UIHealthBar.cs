using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIHealthBar : MonoBehaviour
{
    [Header("Bar")]
    [SerializeField] private RectTransform fillRect;   // the sliced fill image's RectTransform
    [SerializeField] private Image fillImage;           // same object, for colour
    [SerializeField] private TMP_Text healthText;

    [Header("Colours")]
    [SerializeField] private Color fullColor = new Color(0.18f, 0.88f, 0.35f);
    [SerializeField] private Color halfColor = new Color(1f,    0.82f, 0.1f);
    [SerializeField] private Color lowColor  = new Color(0.95f, 0.15f, 0.15f);
    [SerializeField] private float lowThreshold  = 0.3f;
    [SerializeField] private float halfThreshold = 0.6f;

    [Header("Damage Shake")]
    [SerializeField] private float shakeMagnitude = 4f;
    [SerializeField] private float shakeDuration  = 0.25f;

    private PlayerHealth playerHealth;
    private RectTransform rt;           // this object's RectTransform (for shake)
    private Vector2 basePos;
    private float fullWidth;            // cached max width of the fill rect

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) basePos = rt.anchoredPosition;

        // Cache the full width so we can scale it by HP ratio
        if (fillRect != null) fullWidth = fillRect.sizeDelta.x;

        playerHealth = PlayerHealth.Instance ?? FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.OnHealthChanged += UpdateBar;
        playerHealth.OnDamaged       += _ => TriggerShake();
        UpdateBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (playerHealth == null) return;
        playerHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        float t = max > 0 ? (float)current / max : 0f;

        if (fillRect != null)
        {
            // Scale width — sliced image stays crisp at any size
            Vector2 size = fillRect.sizeDelta;
            size.x = fullWidth * t;
            fillRect.sizeDelta = size;
        }

        if (fillImage != null)
            fillImage.color = GetBarColor(t);

        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    private Color GetBarColor(float t)
    {
        if (t <= lowThreshold)
            return lowColor;
        if (t <= halfThreshold)
            return Color.Lerp(lowColor, halfColor, (t - lowThreshold) / (halfThreshold - lowThreshold));
        return Color.Lerp(halfColor, fullColor, (t - halfThreshold) / (1f - halfThreshold));
    }

    private Coroutine shakeRoutine;

    private void TriggerShake()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);
            rt.anchoredPosition = basePos + Random.insideUnitCircle * strength;
            yield return null;
        }
        rt.anchoredPosition = basePos;
        shakeRoutine = null;
    }
}
