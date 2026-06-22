using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private TMP_Text currentAmmoText;
    [SerializeField] private TMP_Text maxAmmoText;
    [SerializeField] private TMP_Text reloadingText;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private UnityEngine.UI.Image ammoIconImage;

    public static AmmoDisplay Instance { get; private set; }

    private Gun activeGun;
    private Coroutine flashRoutine;
    private Coroutine reloadCountRoutine;
    private Coroutine zarkRoutine;
    private bool wasReloading    = false;
    private bool zarkRainbowActive = false;

    private void Awake()
    {
        Instance = this;
        if (playerShooter == null)
            playerShooter = FindAnyObjectByType<PlayerShooter>();
    }

    public void StartZarkRainbow()
    {
        zarkRainbowActive = true;
        if (zarkRoutine != null) StopCoroutine(zarkRoutine);
        zarkRoutine = StartCoroutine(ZarkRainbowRoutine());
    }

    public void StopZarkRainbow()
    {
        zarkRainbowActive = false;
        if (zarkRoutine != null) { StopCoroutine(zarkRoutine); zarkRoutine = null; }
        SetTextColor(normalColor);
        UpdateDisplay();
    }

    private IEnumerator ZarkRainbowRoutine()
    {
        float t = 0f;
        while (zarkRainbowActive)
        {
            t += Time.unscaledDeltaTime * 1.8f;
            if (currentAmmoText != null)
            {
                currentAmmoText.text  = "1";
                currentAmmoText.color = Color.HSVToRGB(t % 1f, 1f, 1f);
            }
            if (maxAmmoText != null)
            {
                maxAmmoText.text  = "1";
                maxAmmoText.color = Color.HSVToRGB((t + 0.33f) % 1f, 1f, 1f);
            }
            yield return null;
        }
    }

    private void Start()
    {
        if (playerShooter != null)
            activeGun = playerShooter.GetActiveGun();

        if (reloadingText != null)
            reloadingText.enabled = false;

        UpdateDisplay();
        SetTextColor(normalColor);
    }

    private void Update()
    {
        if (playerShooter == null)
            return;

        Gun gun = playerShooter.GetActiveGun();

        if (gun != activeGun)
        {
            activeGun = gun;
            wasReloading = false;
            if (reloadCountRoutine != null)
                StopCoroutine(reloadCountRoutine);
            UpdateDisplay();
            return;
        }

        if (activeGun == null)
            return;

        bool isReloading = activeGun.IsReloading;

        if (isReloading && !wasReloading)
        {
            if (reloadCountRoutine != null)
                StopCoroutine(reloadCountRoutine);
            reloadCountRoutine = StartCoroutine(ReloadCountRoutine());
        }

        wasReloading = isReloading;

        if (!isReloading)
            UpdateDisplay();

        if (activeGun.AmmoInMagazine <= 0 && IsFirePressed())
            TriggerFlash();
    }

    private IEnumerator ReloadCountRoutine()
    {
        if (reloadingText != null)
        {
            reloadingText.enabled = true;
            reloadingText.color = normalColor;
        }
        if (reloadingText != null)
            reloadingText.enabled = true;

        float reloadTime = activeGun.GetReloadTime();
        int targetAmmo = activeGun.MagazineSize;
        int startAmmo = activeGun.AmmoInMagazine;
        float elapsed = 0f;

        while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reloadTime);
                int displayAmmo = Mathf.RoundToInt(Mathf.Lerp(startAmmo, targetAmmo, t));
            if (currentAmmoText != null)
                currentAmmoText.text = displayAmmo.ToString();

            yield return null;
        }

        if (currentAmmoText != null)
            currentAmmoText.text = targetAmmo.ToString();

        if (reloadingText != null)
            reloadingText.enabled = false;
        
        if (reloadingText != null)
            reloadingText.color = normalColor;

        reloadCountRoutine = null;
    }

    private void UpdateDisplay()
    {
        if (zarkRainbowActive) return;
        if (activeGun == null)
        {
            if (currentAmmoText != null) currentAmmoText.text = "--";
            if (maxAmmoText != null) maxAmmoText.text = "--";
            if (ammoIconImage != null) ammoIconImage.sprite = null;
            return;
        }

        if (currentAmmoText != null)
            currentAmmoText.text = activeGun.AmmoInMagazine.ToString();

        if (maxAmmoText != null)
            maxAmmoText.text = activeGun.MagazineSize.ToString();

        if (ammoIconImage != null && activeGun.CurrentProfile != null)
        {
            Sprite icon = activeGun.CurrentProfile.AmmoIcon;
            ammoIconImage.sprite = icon;
            ammoIconImage.enabled = icon != null;
        }
    }

    private bool IsFirePressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;
        return false;
    }

    private void TriggerFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetTextColor(emptyColor);
        yield return new WaitForSeconds(flashDuration);
        SetTextColor(normalColor);
        flashRoutine = null;
    }

    private void SetTextColor(Color color)
    {
        if (zarkRainbowActive) return;
        if (currentAmmoText != null) currentAmmoText.color = color;
        if (maxAmmoText != null) maxAmmoText.color = color;
        if (reloadingText != null && reloadingText.enabled) reloadingText.color = color;
    }
}