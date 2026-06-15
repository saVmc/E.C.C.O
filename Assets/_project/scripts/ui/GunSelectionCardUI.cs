using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GunSelectionCardUI : MonoBehaviour
{
    [SerializeField] private Image gunIcon;
    [SerializeField] private TMP_Text gunNameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Vector2 iconSize = new Vector2(120f, 80f);

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
            gunIcon.sprite = profile.WeaponSprite;
            gunIcon.enabled = profile.WeaponSprite != null;
            gunIcon.preserveAspect = false;
            gunIcon.type = Image.Type.Simple;
            RectTransform iconRect = gunIcon.GetComponent<RectTransform>();
            if (iconRect != null)
                iconRect.sizeDelta = iconSize;
            // Force layout groups to respect the desired size
            LayoutElement le = gunIcon.GetComponent<LayoutElement>();
            if (le == null) le = gunIcon.gameObject.AddComponent<LayoutElement>();
            le.minWidth = iconSize.x;
            le.minHeight = iconSize.y;
            le.preferredWidth = iconSize.x;
            le.preferredHeight = iconSize.y;
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
