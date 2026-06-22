using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GunSelectionCardUI : MonoBehaviour
{
    [SerializeField] private Image    gunIcon;
    [SerializeField] private TMP_Text gunNameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button   selectButton;
    [SerializeField] private Image    lockOverlay;   // optional dark overlay image

    private GunProfile         profile;
    private GunSelectionDisplay display;
    private int                cardIndex;

    public void Populate(GunProfile gunProfile, int index, GunSelectionDisplay selectionDisplay)
    {
        profile   = gunProfile;
        cardIndex = index;
        display   = selectionDisplay;

        gameObject.SetActive(true);

        bool locked = !profile.IsUnlocked;

        if (gunIcon != null)
        {
            gunIcon.sprite  = profile.CardIconSprite;
            gunIcon.enabled = profile.CardIconSprite != null;
            gunIcon.color   = locked ? new Color(0.28f, 0.28f, 0.28f, 1f) : Color.white;
        }

        if (gunNameText != null)
            gunNameText.text = locked ? "LOCKED" : profile.DisplayName;

        if (statsText != null)
        {
            if (locked)
            {
                statsText.text = $"Survive wave {profile.WaveLockRequirement} to unlock";
            }
            else
            {
                float fireRate = profile.FireCooldown > 0f ? 1f / profile.FireCooldown : 0f;
                int   damage   = profile.ProjectileProfile != null ? profile.ProjectileProfile.Damage : 0;
                string pelletLine = profile.PelletCount > 1 ? $"\nPellets:       {profile.PelletCount}" : "";

                statsText.text =
                    $"Fire Rate:     {fireRate:F1}/s" +
                    $"\nBase Damage:  {damage}" +
                    pelletLine +
                    $"\nMagazine:     {profile.MagazineSize}" +
                    $"\nReload:         {profile.ReloadTime:F1}s";
            }
        }

        if (lockOverlay != null)
        {
            lockOverlay.gameObject.SetActive(locked);
            lockOverlay.color = new Color(0f, 0f, 0f, 0.65f);
        }

        if (selectButton != null)
        {
            selectButton.interactable = !locked;
            selectButton.onClick.RemoveAllListeners();
            if (!locked) selectButton.onClick.AddListener(OnSelected);
        }
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnSelected()
    {
        display?.SelectGun(cardIndex);
    }
}
