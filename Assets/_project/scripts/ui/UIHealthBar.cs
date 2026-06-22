using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIHealthBar : MonoBehaviour
{
    [Header("HP Bar")]
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthText;

    // Shield bar — created automatically at runtime, no Inspector setup needed
    private RectTransform shieldFillRect;
    private Image         shieldFillImage;

    [Header("Colours")]
    [SerializeField] private Color fullColor   = new Color(0.18f, 0.88f, 0.35f);
    [SerializeField] private Color halfColor   = new Color(1f,    0.82f, 0.1f);
    [SerializeField] private Color lowColor    = new Color(0.95f, 0.15f, 0.15f);
    [SerializeField] private Color shieldColor = new Color(0.1f,  0.85f, 1f,  1f);
    [SerializeField] private float lowThreshold  = 0.3f;
    [SerializeField] private float halfThreshold = 0.6f;

    [Header("Damage Shake")]
    [SerializeField] private float shakeMagnitude = 4f;
    [SerializeField] private float shakeDuration  = 0.25f;

    private PlayerHealth playerHealth;
    private RectTransform rt;
    private Vector2 basePos;
    private float fullWidth;

    private bool  shieldActive;
    private float shieldCurrent;
    private float shieldMax = 1f;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) basePos = rt.anchoredPosition;
        if (fillRect != null) fullWidth = fillRect.sizeDelta.x;

        CreateShieldFill();

        playerHealth = PlayerHealth.Instance ?? FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            playerHealth.OnDamaged       += _ => TriggerShake();
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        ForcefieldAbility.OnShieldChanged += OnShieldChanged;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
        ForcefieldAbility.OnShieldChanged -= OnShieldChanged;
    }

    private void CreateShieldFill()
    {
        if (fillRect == null) return;

        GameObject go = new GameObject("ShieldFill");
        go.transform.SetParent(fillRect.parent, false);
        go.transform.SetAsLastSibling();

        shieldFillRect = go.AddComponent<RectTransform>();
        shieldFillRect.anchorMin        = fillRect.anchorMin;
        shieldFillRect.anchorMax        = fillRect.anchorMax;
        shieldFillRect.anchoredPosition = fillRect.anchoredPosition;
        shieldFillRect.sizeDelta        = fillRect.sizeDelta;
        shieldFillRect.pivot            = fillRect.pivot;

        shieldFillImage       = go.AddComponent<Image>();
        shieldFillImage.color = shieldColor;
        if (fillImage != null) shieldFillImage.sprite = fillImage.sprite;

        go.SetActive(false);
    }

    private void OnShieldChanged(float current, float max)
    {
        shieldCurrent = current;
        shieldMax     = Mathf.Max(1f, max);
        shieldActive  = current > 0f;
        RefreshDisplay();
    }

    private void OnHealthChanged(int current, int max)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        // HP bar always shows actual HP
        if (playerHealth != null)
        {
            float t = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;
            SetFill(t, GetHpColor(t));
        }

        // Shield bar: full blue circle width at full shield, depletes as damage taken
        if (shieldFillRect != null && shieldFillImage != null)
        {
            if (shieldActive)
            {
                shieldFillRect.gameObject.SetActive(true);
                float sf = Mathf.Clamp01(shieldCurrent / shieldMax);
                Vector2 size = shieldFillRect.sizeDelta;
                size.x = fullWidth * sf;
                shieldFillRect.sizeDelta = size;
                shieldFillImage.color    = shieldColor;
            }
            else
            {
                shieldFillRect.gameObject.SetActive(false);
            }
        }

        // Text
        if (healthText != null && playerHealth != null)
        {
            if (shieldActive)
                healthText.text = $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}  +{Mathf.CeilToInt(shieldCurrent)} SHIELD";
            else
                healthText.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        }
    }

    private void SetFill(float t, Color color)
    {
        if (fillRect != null)
        {
            Vector2 size = fillRect.sizeDelta;
            size.x = fullWidth * Mathf.Clamp01(t);
            fillRect.sizeDelta = size;
        }
        if (fillImage != null) fillImage.color = color;
    }

    private Color GetHpColor(float t)
    {
        if (t <= lowThreshold)  return lowColor;
        if (t <= halfThreshold) return Color.Lerp(lowColor,  halfColor, (t - lowThreshold)  / (halfThreshold - lowThreshold));
        return                         Color.Lerp(halfColor, fullColor,  (t - halfThreshold) / (1f - halfThreshold));
    }

    private Coroutine shakeRoutine;

    private void TriggerShake()
    {
        if (!shieldActive)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine());
        }
    }

    private IEnumerator ShakeRoutine()
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            rt.anchoredPosition = basePos + Random.insideUnitCircle * Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);
            yield return null;
        }
        rt.anchoredPosition = basePos;
        shakeRoutine = null;
    }
}
