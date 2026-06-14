using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AbilityInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private AbilityInfoRow[] abilityRows; // 4 rows for abilities
    [SerializeField] private AbilityInfoRow gunRow;
    [SerializeField] private GameObject divider;           // line between abilities and gun

    [Header("Description Panel")]
    [SerializeField] private TMP_Text descriptionTitle;
    [SerializeField] private TMP_Text descriptionBody;

    [Header("Overlay")]
    [SerializeField] private CanvasGroup darkOverlay;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("Close Button")]
    [SerializeField] private UnityEngine.UI.Button closeButton;

    private const string DefaultTitle = "Select an upgrade";
    private const string DefaultBody  = "Click any unlocked ★ to see what that upgrade adds.";

    private bool isOpen = false;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (darkOverlay != null) { darkOverlay.alpha = 0f; darkOverlay.gameObject.SetActive(false); }
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();
    }

    private void Toggle()
    {
        if (isOpen) Close(); else Open();
    }

    private void Open()
    {
        isOpen = true;
        panelRoot.SetActive(true);
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.alpha = overlayAlpha;
        }
        Time.timeScale = 0f;
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        panelRoot.SetActive(false);
        if (darkOverlay != null)
        {
            darkOverlay.alpha = 0f;
            darkOverlay.gameObject.SetActive(false);
        }
        Time.timeScale = 1f;
    }

    private void Refresh()
    {
        ShowDefaultDescription();

        // Hide all ability rows first
        foreach (AbilityInfoRow row in abilityRows)
            row.Hide();

        // Populate from ability slots
        if (AbilityManager.Instance != null)
        {
            int rowIndex = 0;
            foreach (AbilitySlot slot in AbilityManager.Instance.Slots)
            {
                if (!slot.IsEmpty && rowIndex < abilityRows.Length)
                {
                    abilityRows[rowIndex].PopulateAbility(slot.Ability, this);
                    rowIndex++;
                }
            }
        }

        // Gun row
        PlayerShooter shooter = FindAnyObjectByType<PlayerShooter>();
        Gun gun = shooter != null ? shooter.GetActiveGun() : null;
        bool hasGun = gun != null && gun.CurrentProfile != null;

        if (gunRow != null)
        {
            if (hasGun)
                gunRow.PopulateGun(gun, this);
            else
                gunRow.Hide();
        }

        if (divider != null)
            divider.SetActive(hasGun);
    }

    public void ShowDescription(string abilityName, AbilityDefinition def)
    {
        if (descriptionTitle != null) descriptionTitle.text = def.DisplayName;
        if (descriptionBody != null)  descriptionBody.text = def.Description;
    }

    public void ShowGunUpgradeDescription(string gunName, GunUpgrade upgrade)
    {
        if (descriptionTitle != null) descriptionTitle.text = upgrade.DisplayName;
        if (descriptionBody != null)  descriptionBody.text = upgrade.Description;
    }

    private void ShowDefaultDescription()
    {
        if (descriptionTitle != null) descriptionTitle.text = DefaultTitle;
        if (descriptionBody != null)  descriptionBody.text  = DefaultBody;
    }
}
