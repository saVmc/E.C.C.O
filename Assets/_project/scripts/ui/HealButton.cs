using UnityEngine;
using TMPro;

public sealed class HealButton : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private LevelUpDisplay levelUpDisplay;

    private void Start()
    {
        gameObject.SetActive(false);

        if (levelUpDisplay == null)
            levelUpDisplay = FindAnyObjectByType<LevelUpDisplay>();

        if (levelUpDisplay == null)
            levelUpDisplay = GetComponentInParent<LevelUpDisplay>();

        if (levelUpDisplay != null)
        {
            levelUpDisplay.OnLevelUpShown  += Show;
            levelUpDisplay.OnLevelUpHidden += Hide;
        }
        else
        {
            Debug.LogWarning("[HealButton] Could not find LevelUpDisplay!");
        }
    }

    private void OnDestroy()
    {
        if (levelUpDisplay != null)
        {
            levelUpDisplay.OnLevelUpShown  -= Show;
            levelUpDisplay.OnLevelUpHidden -= Hide;
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide() => gameObject.SetActive(false);

    public void Heal()
    {
        if (PlayerHealth.Instance == null) return;
        int amount = Mathf.CeilToInt(PlayerHealth.Instance.MaxHealth * 0.1f);
        PlayerHealth.Instance.Heal(amount);
        levelUpDisplay?.HideCards();
    }
}

