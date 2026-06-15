using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class GunSelectionDisplay : MonoBehaviour
{
    [SerializeField] private WeaponDraftManager draftManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GunSelectionCardUI[] cards;
    [SerializeField] private TMP_Text titleText;

    [Header("Overlay")]
    [SerializeField] private CanvasGroup darkOverlay;
    [SerializeField] private float overlayAlpha = 0.75f;
    [SerializeField] private float overlayFadeDuration = 0.4f;

    [Header("Card Fade")]
    [SerializeField] private float cardFadeDuration = 0.35f;

    private IReadOnlyList<GunProfile> choices;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (darkOverlay != null) { darkOverlay.alpha = 0f; darkOverlay.gameObject.SetActive(false); }
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (draftManager != null)
            choices = draftManager.GenerateChoices();

        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // Fade in overlay
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < overlayFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                darkOverlay.alpha = Mathf.Lerp(0f, overlayAlpha, elapsed / overlayFadeDuration);
                yield return null;
            }
            darkOverlay.alpha = overlayAlpha;
        }

        // Populate and show panel
        if (titleText != null) { titleText.gameObject.SetActive(true); titleText.text = "GUN SELECTION"; }

        foreach (GunSelectionCardUI card in cards)
            card.Hide();

        if (choices != null)
        {
            for (int i = 0; i < cards.Length && i < choices.Count; i++)
                cards[i].Populate(choices[i], i, this);
        }

        if (panelRoot != null) panelRoot.SetActive(true);

        // Fade in each card
        CanvasGroup[] cardGroups = new CanvasGroup[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            cardGroups[i] = cards[i].GetComponent<CanvasGroup>();
            if (cardGroups[i] == null)
                cardGroups[i] = cards[i].gameObject.AddComponent<CanvasGroup>();
            cardGroups[i].alpha = 0f;
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < cardFadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, fadeElapsed / cardFadeDuration);
            foreach (CanvasGroup cg in cardGroups)
                if (cg != null) cg.alpha = t;
            yield return null;
        }

        foreach (CanvasGroup cg in cardGroups)
            if (cg != null) cg.alpha = 1f;
    }

    public void SelectGun(int index)
    {
        draftManager?.ChooseWeapon(index);
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(false);

        if (darkOverlay != null)
        {
            float elapsed = 0f;
            while (elapsed < overlayFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                darkOverlay.alpha = Mathf.Lerp(overlayAlpha, 0f, elapsed / overlayFadeDuration);
                yield return null;
            }
            darkOverlay.alpha = 0f;
            darkOverlay.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
    }
}
