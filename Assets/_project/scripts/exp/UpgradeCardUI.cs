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
            starText.text = BuildStarString(offer.StarLevel);

        if (upgradeNameText != null)
            upgradeNameText.text = offer.UpgradeName;

        if (descriptionText != null)
            descriptionText.text = offer.Description;

        if (newAbilityBackground != null) newAbilityBackground.gameObject.SetActive(false);
        if (upgradeBackground != null) upgradeBackground.gameObject.SetActive(true);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnGunUpgradeSelected);
        }
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