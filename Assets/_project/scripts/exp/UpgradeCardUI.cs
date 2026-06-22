using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text abilityNameText;
    [SerializeField] private TMP_Text starText;
    [SerializeField] private TMP_Text upgradeNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image newAbilityBackground;
    [SerializeField] private Image upgradeBackground;
    [SerializeField] private Image maxedBackground;

    private UpgradeOffer offer_field;
    private LevelUpDisplay display;
    private GunUpgradeOffer gunOffer;

    public void Populate(UpgradeOffer upgradeOffer, LevelUpDisplay levelUpDisplay)
    {
        if (upgradeOffer == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (upgradeOffer.IsHealOffer)
        {
            PopulateHealOffer(levelUpDisplay);
            return;
        }

        if (upgradeOffer.IsPrestigeOffer)
        {
            PopulatePrestigeOffer(upgradeOffer, levelUpDisplay);
            return;
        }

        if (upgradeOffer.IsGunUpgrade)
        {
            PopulateGunUpgrade(upgradeOffer.GunUpgrade, levelUpDisplay);
            return;
        }

        if (upgradeOffer.Definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        offer_field = upgradeOffer;
        display = levelUpDisplay;

        if (offer_field == null || offer_field.Definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (newAbilityBackground != null)
            newAbilityBackground.gameObject.SetActive(offer_field.IsNewAbility);

        if (upgradeBackground != null)
            upgradeBackground.gameObject.SetActive(!offer_field.IsNewAbility);
        
        bool isMaxed = !upgradeOffer.IsNewAbility && !upgradeOffer.IsGunUpgrade
    && upgradeOffer.Definition != null
    && upgradeOffer.Definition.IsMaxStar;

Debug.Log($"[UpgradeCardUI] {upgradeOffer.Definition?.DisplayName} | isMaxed={isMaxed} | IsMaxStar={upgradeOffer.Definition?.IsMaxStar} | maxedBackground assigned={maxedBackground != null}");

if (maxedBackground != null)
    maxedBackground.gameObject.SetActive(isMaxed);

if (upgradeBackground != null)
    upgradeBackground.gameObject.SetActive(!offer_field.IsNewAbility && !isMaxed);

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = offer_field.Definition.Icon;
            iconImage.enabled = offer_field.Definition.Icon != null;
        }

        if (abilityNameText != null)
            abilityNameText.text = offer_field.Definition.AbilityName;

        if (starText != null)
            starText.text = BuildStarString(offer_field.Definition.StarLevel);

        if (upgradeNameText != null)
            upgradeNameText.text = offer_field.Definition.DisplayName;

        if (descriptionText != null)
            descriptionText.text = offer_field.Definition.Description;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelected);
        }

        // Force layout rebuild for this card
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void PopulateHealOffer(LevelUpDisplay levelUpDisplay)
    {
        offer_field = null;
        gunOffer = null;
        display = levelUpDisplay;
        gameObject.SetActive(true);

        if (iconImage != null) iconImage.enabled = false;
        if (abilityNameText != null) abilityNameText.text = "MEDKIT";
        if (starText != null) starText.text = "✚✚✚✚✚";
        if (upgradeNameText != null) upgradeNameText.text = "Emergency Heal";

        int healAmount = PlayerHealth.Instance != null
            ? Mathf.CeilToInt(PlayerHealth.Instance.MaxHealth * 0.1f)
            : 0;
        if (descriptionText != null)
            descriptionText.text = $"Sacrifice an upgrade. Restore {healAmount} HP ({(PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHealth : 0)}/{(PlayerHealth.Instance != null ? PlayerHealth.Instance.MaxHealth : 0)}).";

        if (newAbilityBackground != null) newAbilityBackground.gameObject.SetActive(false);
        if (upgradeBackground != null) upgradeBackground.gameObject.SetActive(true);
        if (maxedBackground != null) maxedBackground.gameObject.SetActive(false);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                if (PlayerHealth.Instance != null)
                    PlayerHealth.Instance.Heal(Mathf.CeilToInt(PlayerHealth.Instance.MaxHealth * 0.1f));
                levelUpDisplay.HideCards();
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void PopulateGunUpgrade(GunUpgradeOffer offer, LevelUpDisplay display)
    {
        gunOffer = offer;
        this.display = display;
        offer_field = null;

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = offer.Icon;
            iconImage.enabled = offer.Icon != null;
        }

        if (abilityNameText != null)
            abilityNameText.text = offer.GunName;

        if (starText != null)
            starText.text = BuildStarString(gunOffer.StarLevel);

        if (upgradeNameText != null)
            upgradeNameText.text = offer.UpgradeName;

        if (descriptionText != null)
            descriptionText.text = offer.Description;

        bool isGunMaxed = offer.IsMaxStar;
        if (newAbilityBackground != null) newAbilityBackground.gameObject.SetActive(false);
        if (maxedBackground != null) maxedBackground.gameObject.SetActive(isGunMaxed);
        if (upgradeBackground != null) upgradeBackground.gameObject.SetActive(!isGunMaxed);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnGunUpgradeSelected);
        }

        // Force layout rebuild for this card
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void PopulatePrestigeOffer(UpgradeOffer offer, LevelUpDisplay levelUpDisplay)
    {
        offer_field = null;
        gunOffer    = null;
        display     = levelUpDisplay;
        gameObject.SetActive(true);

        var data = PrestigeEffects.GetData(offer.PrestigeType.Value);

        if (iconImage != null) iconImage.enabled = false;
        if (abilityNameText != null) abilityNameText.text  = "PRESTIGE";
        if (starText != null)        starText.text         = "★★★★★";
        if (upgradeNameText != null) upgradeNameText.text  = data.Title;
        if (descriptionText != null) descriptionText.text  = data.Description;

        if (newAbilityBackground != null) newAbilityBackground.gameObject.SetActive(false);
        if (upgradeBackground    != null) upgradeBackground.gameObject.SetActive(false);
        if (maxedBackground      != null) maxedBackground.gameObject.SetActive(true);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            PrestigeUpgradeType typeCapture = offer.PrestigeType.Value;
            selectButton.onClick.AddListener(() =>
            {
                PrestigeEffects.Apply(typeCapture);
                levelUpDisplay.HideCards();
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    private void OnGunUpgradeSelected()
    {
        if (gunOffer == null || display == null) return;

        Gun activeGun = FindAnyObjectByType<PlayerShooter>()?.GetActiveGun();
        gunOffer.Apply(activeGun);
        display.HideCards();
    }

    private void OnSelected()
    {
        if (offer_field == null || display == null)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && AbilityManager.Instance != null)
            offer_field.Apply(AbilityManager.Instance, player);

        display.HideCards();
    }

    private string BuildStarString(int starLevel)
    {
        string result = "";
        for (int i = 0; i < 5; i++)
            result += i < starLevel ? "★" : "☆";
        return result;
    }
}