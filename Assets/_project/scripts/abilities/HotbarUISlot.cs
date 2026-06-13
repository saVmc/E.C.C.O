using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityHotbarSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0f, 0f, 0f, 0.6f);

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
        if (slot == null)
            return;

        Refresh();
    }

    private void Refresh()
    {
        bool isEmpty = slot.IsEmpty;
        bool isReady = !isEmpty && slot.Ability.IsReady;
        float progress = isEmpty ? 1f : slot.Ability.CooldownProgress;

        if (iconImage != null)
        {
            if (isEmpty || slot.Ability.Definition == null || slot.Ability.Definition.Icon == null)
            {
                iconImage.sprite = null;
                iconImage.color = emptyColor;
            }
            else
            {
                iconImage.sprite = slot.Ability.Definition.Icon;
                iconImage.color = readyColor;
            }
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(!isEmpty && !isReady);
            cooldownOverlay.fillAmount = 1f - progress;
            cooldownOverlay.color = cooldownColor;
        }

        if (cooldownText != null)
        {
            if (!isEmpty && !isReady)
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