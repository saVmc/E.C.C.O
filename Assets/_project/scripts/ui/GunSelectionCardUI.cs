using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GunSelectionCardUI : MonoBehaviour
{
    [SerializeField] private Image gunIcon;
    [SerializeField] private TMP_Text gunNameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button selectButton;


    private GunProfile profile;
    private GunSelectionDisplay display;
    private int cardIndex;

    public void Populate(GunProfile gunProfile, int index, GunSelectionDisplay selectionDisplay)
    {
        profile = gunProfile;
        cardIndex = index;
        display = selectionDisplay;

        gameObject.SetActive(true);

        if (gunIcon != null)
        {
            gunIcon.sprite = profile.CardIconSprite;
            gunIcon.enabled = profile.CardIconSprite != null;
        }

        if (gunNameText != null)
            gunNameText.text = profile.DisplayName;

        if (statsText != null)
        {
            float fireRate = profile.FireCooldown > 0f ? 1f / profile.FireCooldown : 0f;
            int damage = profile.ProjectileProfile != null ? profile.ProjectileProfile.Damage : 0;
            string pelletLine = profile.PelletCount > 1 ? $"\nPellets:       {profile.PelletCount}" : "";

            statsText.text =
                $"Fire Rate:     {fireRate:F1}/s" +
                $"\nBase Damage:  {damage}" +
                pelletLine +
                $"\nMagazine:     {profile.MagazineSize}" +
                $"\nReload:         {profile.ReloadTime:F1}s";
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelected);
        }
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnSelected()
    {
        display?.SelectGun(cardIndex);
    }
}
