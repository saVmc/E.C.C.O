using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityInfoRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button[] starButtons; // 5 buttons, one per star

    [SerializeField] private Color starUnlockedColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color starLockedColor   = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private AbilityInfoPanel panel;

    public void PopulateAbility(Ability ability, AbilityInfoPanel infoPanel)
    {
        panel = infoPanel;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = ability.Definition.Icon;
            iconImage.enabled = ability.Definition.Icon != null;
        }

        if (nameText != null)
            nameText.text = ability.Definition.AbilityName;

        // Walk upgrade chain from base definition to collect all star definitions
        AbilityDefinition[] starDefs = BuildChain(ability.BaseDefinition);
        int currentStar = ability.Definition.StarLevel;

        for (int i = 0; i < starButtons.Length; i++)
        {
            if (starButtons[i] == null) continue;

            // Star index i corresponds to star level i+1 (star 0 = base, stars 1-5 = upgrades)
            int starLevel = i + 1;
            bool unlocked = currentStar >= starLevel;
            AbilityDefinition starDef = starLevel < starDefs.Length ? starDefs[starLevel] : null;

            starButtons[i].interactable = unlocked && starDef != null;

            TMP_Text starLabel = starButtons[i].GetComponentInChildren<TMP_Text>();
            if (starLabel != null)
            {
                starLabel.text = unlocked ? "★" : "☆";
                starLabel.color = unlocked ? starUnlockedColor : starLockedColor;
            }

            // Capture for lambda
            AbilityDefinition captured = starDef;
            string abilityName = ability.Definition.AbilityName;
            starButtons[i].onClick.RemoveAllListeners();
            if (unlocked && captured != null)
                starButtons[i].onClick.AddListener(() => panel.ShowDescription(abilityName, captured));
        }
    }

    public void PopulateGun(Gun gun, AbilityInfoPanel infoPanel)
    {
        panel = infoPanel;
        gameObject.SetActive(true);

        GunProfile profile = gun.CurrentProfile;

        if (iconImage != null)
        {
            iconImage.sprite = profile != null ? profile.WeaponSprite : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (nameText != null)
            nameText.text = profile != null ? profile.DisplayName : "Weapon";

        int appliedCount = gun.appliedUpgrades.Count;

        for (int i = 0; i < starButtons.Length; i++)
        {
            if (starButtons[i] == null) continue;

            int starIndex = i; // upgrade index (0-based)
            bool unlocked = starIndex < appliedCount;
            GunUpgrade upgrade = unlocked ? gun.appliedUpgrades[starIndex] : null;

            starButtons[i].interactable = unlocked && upgrade != null;

            TMP_Text starLabel = starButtons[i].GetComponentInChildren<TMP_Text>();
            if (starLabel != null)
            {
                starLabel.text = unlocked ? "★" : "☆";
                starLabel.color = unlocked ? starUnlockedColor : starLockedColor;
            }

            GunUpgrade captured = upgrade;
            string gunName = profile != null ? profile.DisplayName : "Weapon";
            starButtons[i].onClick.RemoveAllListeners();
            if (unlocked && captured != null)
                starButtons[i].onClick.AddListener(() => panel.ShowGunUpgradeDescription(gunName, captured));
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Walks the full chain from the base definition and returns as array indexed by star level
    private AbilityDefinition[] BuildChain(AbilityDefinition baseDef)
    {
        AbilityDefinition[] chain = new AbilityDefinition[6]; // index 0-5
        AbilityDefinition current = baseDef;
        while (current != null)
        {
            if (current.StarLevel >= 0 && current.StarLevel < chain.Length)
                chain[current.StarLevel] = current;
            current = current.NextStarDefinition;
        }
        return chain;
    }
}
