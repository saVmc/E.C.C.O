using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpDisplay : MonoBehaviour
{
    [SerializeField] private GameObject cardContainer;
    [SerializeField] private List<UpgradeCardUI> cards = new List<UpgradeCardUI>();
    [SerializeField] private float delayBeforeCards = 0.4f;
    [SerializeField] private float cardSlideDistance = 300f;
    [SerializeField] private float cardAnimDuration = 0.25f;
    [SerializeField] private CanvasGroup darkOverlay;
    [SerializeField] private float overlayAlpha = 1f;
    [SerializeField] private float overlayFadeDuration = 0.5f;
    [SerializeField] private TMP_Text levelUpText;
    [SerializeField] private float levelUpTextDuration = 0.8f;
    [SerializeField] private HorizontalLayoutGroup cardLayout;
    [SerializeField] private TMP_Text pickPromptText;
    [SerializeField] private float pickPromptDelay = 0.3f;

    private RectTransform[] cardTransforms;
    private Vector2[] cardRestPositions;
    private bool isShowing = false;
    private List<PulseUI> cardPulsers = new List<PulseUI>();

    private int pendingLevelUps = 0;

    private void Awake()
    {
        if (cardContainer != null)
        {
            cardTransforms = new RectTransform[cards.Count];
            cardRestPositions = new Vector2[cards.Count];

            for (int i = 0; i < cards.Count; i++)
            {
                cardTransforms[i] = cards[i].GetComponent<RectTransform>();

                PulseUI pulser = cards[i].GetComponent<PulseUI>();
                if (pulser == null)
                    pulser = cards[i].gameObject.AddComponent<PulseUI>();
                pulser.SetPulsing(false);
                cardPulsers.Add(pulser);
            }

            cardContainer.SetActive(false);
        }

        if (levelUpText != null)
            levelUpText.gameObject.SetActive(false);

        if (pickPromptText != null)
            pickPromptText.gameObject.SetActive(false);

        if (darkOverlay != null)
        {
            darkOverlay.alpha = 0f;
            darkOverlay.gameObject.SetActive(false);
        }
    }

    private void OnEnable() { }

    private void OnDisable()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        if (isShowing)
        {
            pendingLevelUps++;
            return;
        }
        isShowing = true;
        StartCoroutine(ShowLevelUpRoutine(newLevel));
    }

    private IEnumerator ShowLevelUpRoutine(int newLevel)
    {
        Debug.Log($"ShowLevelUpRoutine started, isShowing was: {isShowing}", this);
        isShowing = true;

        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.alpha = 0f;
            float fadeElapsed = 0f;
            while (fadeElapsed < overlayFadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                darkOverlay.alpha = Mathf.Lerp(0f, overlayAlpha, fadeElapsed / overlayFadeDuration);
                yield return null;
            }
            darkOverlay.alpha = overlayAlpha;
        }

        // Show text
        if (levelUpText != null)
        {
            levelUpText.text = $"LEVEL UP!";
            levelUpText.gameObject.SetActive(true);
            levelUpText.alpha = 0f;

            float textElapsed = 0f;
            float halfDuration = levelUpTextDuration * 0.5f;

            while (textElapsed < halfDuration)
            {
                textElapsed += Time.unscaledDeltaTime;
                levelUpText.alpha = Mathf.Lerp(0f, 1f, textElapsed / halfDuration);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(halfDuration);
        }
        else
        {
            yield return new WaitForSecondsRealtime(delayBeforeCards);
        }

        if (AbilityManager.Instance != null)
            AbilityManager.Instance.enabled = false;

        Time.timeScale = 0f;

        List<UpgradeOffer> offers = AbilityManager.Instance != null
            ? AbilityManager.Instance.GenerateUpgradeOffers(3)
            : new List<UpgradeOffer>();

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < offers.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Populate(offers[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
        
        Debug.Log($"Offers generated: {offers.Count}");
        for (int i = 0; i < offers.Count; i++)
            Debug.Log($"Offer {i}: {(offers[i].IsGunUpgrade ? offers[i].GunUpgrade.UpgradeName : offers[i].Definition?.DisplayName)}");

        if (cardContainer != null)
            cardContainer.SetActive(true);

        if (cardLayout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardLayout.GetComponent<RectTransform>());

        for (int i = 0; i < cardTransforms.Length; i++)
        {
            if (cardTransforms[i].gameObject.activeSelf)
            {
                cardRestPositions[i] = cardTransforms[i].anchoredPosition;
                cardTransforms[i].anchoredPosition = cardRestPositions[i] + Vector2.down * cardSlideDistance;
            }
        }

        float elapsed = 0f;
        while (elapsed < cardAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cardAnimDuration);
            for (int i = 0; i < cardTransforms.Length; i++)
            {
                if (cardTransforms[i].gameObject.activeSelf)
                    cardTransforms[i].anchoredPosition = Vector2.Lerp(
                        cardRestPositions[i] + Vector2.down * cardSlideDistance,
                        cardRestPositions[i],
                        t
                    );
            }
            yield return null;
        }

        if (pickPromptText != null)
            StartCoroutine(FadeInPickPrompt());

        foreach (PulseUI pulser in cardPulsers)
            pulser.SetPulsing(true);
    }

    private IEnumerator FadeInPickPrompt()
    {
        if (pickPromptText == null) yield break;
        pickPromptText.gameObject.SetActive(true);
        pickPromptText.alpha = 0f;
        yield return new WaitForSecondsRealtime(pickPromptDelay);
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            pickPromptText.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
            yield return null;
        }
    }

    public void HideCards()
    {
        foreach (PulseUI pulser in cardPulsers)
            pulser.SetPulsing(false);

        if (cardContainer != null)
            cardContainer.SetActive(false);
        
        if (levelUpText != null)
            levelUpText.gameObject.SetActive(false);

        if (pickPromptText != null)
            pickPromptText.gameObject.SetActive(false);

        if (darkOverlay != null)
        {
            darkOverlay.alpha = 0f;
            darkOverlay.gameObject.SetActive(false);
        }

        if (AbilityManager.Instance != null)
            AbilityManager.Instance.enabled = true;

        Time.timeScale = 1f;
        isShowing = false;

        if (pendingLevelUps > 0)
        {
            pendingLevelUps--;
            isShowing = true;
            StartCoroutine(ShowLevelUpRoutine(PlayerProgression.Instance.CurrentLevel));
        }
    }
}