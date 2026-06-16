using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all boss-related UI: spawn banner, health bar, off-screen arrow indicator.
/// Attach to a Canvas child GameObject. Wire the serialised fields in the Inspector.
/// </summary>
public sealed class BossHUD : MonoBehaviour
{
    [Header("Spawn Banner")]
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private float bannerHoldDuration = 1.8f;
    [SerializeField] private float bannerFadeDuration = 0.7f;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthLabel;

    [Header("Off-screen Arrow")]
    [SerializeField] private RectTransform arrowIndicator;
    [SerializeField] private float edgePadding = 64f;

    private Enemy activeBoss;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        if (bannerText != null)   bannerText.gameObject.SetActive(false);
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);

        if (HordeSpawner.Instance != null)
        {
            HordeSpawner.Instance.OnBossSpawned += OnBossSpawned;
            HordeSpawner.Instance.OnBossKilled  += OnBossKilled;
        }
    }

    private void OnDestroy()
    {
        if (HordeSpawner.Instance != null)
        {
            HordeSpawner.Instance.OnBossSpawned -= OnBossSpawned;
            HordeSpawner.Instance.OnBossKilled  -= OnBossKilled;
        }
    }

    private void OnBossSpawned(int bossNumber)
    {
        activeBoss = HordeSpawner.Instance.ActiveBoss;
        if (healthBarRoot != null) healthBarRoot.SetActive(true);
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(true);
        if (bannerText != null) StartCoroutine(BannerRoutine(bossNumber));
    }

    private void OnBossKilled()
    {
        activeBoss = null;
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
    }

    private IEnumerator BannerRoutine(int bossNumber)
    {
        bannerText.gameObject.SetActive(true);
        bannerText.text = bossNumber == 1 ? "BOSS SPAWNED!" : $"BOSS {bossNumber} SPAWNED!";
        bannerText.alpha = 1f;

        yield return new WaitForSecondsRealtime(bannerHoldDuration);

        float t = 0f;
        while (t < bannerFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            bannerText.alpha = 1f - t / bannerFadeDuration;
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (activeBoss == null || activeBoss.IsDead)
        {
            if (activeBoss != null && activeBoss.IsDead) OnBossKilled();
            return;
        }

        UpdateHealthBar();
        UpdateArrow();
    }

    private void UpdateHealthBar()
    {
        if (healthFill == null) return;
        float ratio = Mathf.Clamp01(activeBoss.CurrentHealth / activeBoss.MaxHealth);
        healthFill.fillAmount = ratio;
        if (healthLabel != null)
            healthLabel.text = $"{Mathf.CeilToInt(activeBoss.CurrentHealth)} / {Mathf.CeilToInt(activeBoss.MaxHealth)}";
    }

    private void UpdateArrow()
    {
        if (arrowIndicator == null || mainCam == null) return;

        Vector3 vp = mainCam.WorldToViewportPoint(activeBoss.transform.position);
        bool onScreen = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
        arrowIndicator.gameObject.SetActive(!onScreen);
        if (onScreen) return;

        // Direction from screen centre to boss
        Vector2 screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 bossScreen   = (Vector2)mainCam.WorldToScreenPoint(activeBoss.transform.position) - screenCentre;
        Vector2 dir = bossScreen.normalized;

        // Clamp to screen rectangle edge
        float halfW = Screen.width  * 0.5f - edgePadding;
        float halfH = Screen.height * 0.5f - edgePadding;
        Vector2 pos;
        if (Mathf.Abs(dir.x) * halfH > Mathf.Abs(dir.y) * halfW)
            pos = dir * (halfW / Mathf.Abs(dir.x));
        else
            pos = dir * (halfH / Mathf.Abs(dir.y));

        arrowIndicator.anchoredPosition = pos;
        arrowIndicator.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }
}
