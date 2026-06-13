using System.Collections;
using UnityEngine;

public class LevelUpDisplay : MonoBehaviour
{
    [SerializeField] private GameObject cardContainer;
    [SerializeField] private float delayBeforeCards = 0.4f;
    [SerializeField] private float cardSlideDistance = 300f;
    [SerializeField] private float cardAnimDuration = 0.3f;

    private RectTransform[] cards;
    private Vector2[] cardRestPositions;
    private bool isShowing = false;

    private void Awake()
    {
        if (cardContainer != null)
        {
            cards = cardContainer.GetComponentsInChildren<RectTransform>(true);
            cardRestPositions = new Vector2[cards.Length];
            for (int i = 0; i < cards.Length; i++)
                cardRestPositions[i] = cards[i].anchoredPosition;

            cardContainer.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (isShowing)
            return;

        StartCoroutine(ShowLevelUpRoutine());
    }

    private IEnumerator ShowLevelUpRoutine()
    {
        isShowing = true;

        yield return new WaitForSecondsRealtime(delayBeforeCards);

        Time.timeScale = 0f;

        if (cardContainer != null)
            cardContainer.SetActive(true);

        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
                cards[i].anchoredPosition = cardRestPositions[i] + Vector2.down * cardSlideDistance;

            float elapsed = 0f;
            while (elapsed < cardAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / cardAnimDuration);
                for (int i = 0; i < cards.Length; i++)
                    cards[i].anchoredPosition = Vector2.Lerp(
                        cardRestPositions[i] + Vector2.down * cardSlideDistance,
                        cardRestPositions[i],
                        t
                    );
                yield return null;
            }
        }
    }

    public void HideCards()
    {
        if (cardContainer != null)
            cardContainer.SetActive(false);

        Time.timeScale = 1f;
        isShowing = false;
    }
}