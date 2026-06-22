using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityHotbarSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private Color emptyColor    = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color readyColor    = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0f, 0f, 0f, 0.6f);

    // Blue pulse colours for the "recording — press again to stop" state
    private static readonly Color RecordPulseA = new Color(0.30f, 0.80f, 1.00f, 1.00f);
    private static readonly Color RecordPulseB = new Color(0.80f, 0.95f, 1.00f, 1.00f);

    private AbilitySlot slot;

    public void Initialise(AbilitySlot abilitySlot)
    {
        slot = abilitySlot;

        if (hotkeyText != null)
            hotkeyText.text = slot.HotKey.ToString();

        if (cooldownOverlay != null)
            cooldownOverlay.fillMethod = Image.FillMethod.Vertical;

        Refresh();
    }

    private void Update()
    {
        if (slot == null) return;
        Refresh();
    }

    private void Refresh()
    {
        bool isEmpty  = slot.IsEmpty;
        bool isActive = !isEmpty && slot.Ability.IsInActiveState;
        bool isReady  = !isEmpty && !isActive && slot.Ability.IsReady;
        float progress = isEmpty ? 1f : slot.Ability.CooldownProgress;

        // ── Icon — never change colour here, let the overlay do the glow ─────
        if (iconImage != null)
        {
            bool hasIcon = !isEmpty && slot.Ability.Definition != null && slot.Ability.Definition.Icon != null;
            iconImage.sprite = hasIcon ? slot.Ability.Definition.Icon : null;
            iconImage.color  = isEmpty ? emptyColor : readyColor;
        }

        // ── Cooldown / glow overlay ───────────────────────────────────────────
        if (cooldownOverlay != null)
        {
            if (isActive)
            {
                // Repurpose the overlay as a pulsing cyan tint instead of a dark block
                float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = 1f;
                cooldownOverlay.color = new Color(RecordPulseA.r, RecordPulseA.g, RecordPulseA.b,
                                                  Mathf.Lerp(0.15f, 0.45f, pulse));
            }
            else if (!isEmpty && !isReady)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = 1f - progress;
                cooldownOverlay.color      = cooldownColor;
            }
            else
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
        }

        // ── Cooldown / state text ─────────────────────────────────────────────
        if (cooldownText != null)
        {
            if (isActive)
            {
                cooldownText.gameObject.SetActive(false);
            }
            else if (!isEmpty && !isReady)
            {
                float remaining = slot.Ability.Definition.Cooldown * (1f - progress);
                cooldownText.text = remaining > 1f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : remaining.ToString("F1");
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}
