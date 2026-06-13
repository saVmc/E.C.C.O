using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpBar : MonoBehaviour
{
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private float smoothSpeed = 5f;

    private float targetFill = 0f;

    private void OnEnable()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.OnExpChanged += HandleExpChanged;
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;
        }
    }

    private void Start()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.OnExpChanged -= HandleExpChanged;
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
            PlayerProgression.Instance.OnExpChanged += HandleExpChanged;
            PlayerProgression.Instance.OnLevelUp += HandleLevelUp;
            RefreshAll();
        }
        else
        {
            Debug.LogWarning("ExpBar: PlayerProgression.Instance is null — is PlayerProgression in the scene?");
        }
    }

    private void OnDisable()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.OnExpChanged -= HandleExpChanged;
            PlayerProgression.Instance.OnLevelUp -= HandleLevelUp;
        }
    }

    private void Update()
    {
        if (expSlider != null)
            expSlider.value = Mathf.Lerp(expSlider.value, targetFill, smoothSpeed * Time.deltaTime);
    }

    private void HandleExpChanged(int currentExp, int expToNextLevel)
    {
        targetFill = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;

        if (expText != null)
            expText.text = $"{currentExp} / {expToNextLevel}";
    }

    private void HandleLevelUp(int newLevel)
    {
        if (expSlider != null)
            expSlider.value = 0f;

        targetFill = 0f;
        RefreshLevelText();
    }

    private void RefreshAll()
    {
        if (PlayerProgression.Instance == null)
            return;

        int currentExp = PlayerProgression.Instance.CurrentExp;
        int expToNext = PlayerProgression.Instance.ExpToNextLevel;
        int level = PlayerProgression.Instance.CurrentLevel;

        targetFill = expToNext > 0 ? (float)currentExp / expToNext : 0f;

        if (expSlider != null)
            expSlider.value = targetFill;

        if (expText != null)
            expText.text = $"{currentExp} / {expToNext}";

        RefreshLevelText();
    }

    private void RefreshLevelText()
{
    if (PlayerProgression.Instance == null)
        return;

    int level = PlayerProgression.Instance.CurrentLevel;

    if (currentLevelText != null)
        currentLevelText.text = level.ToString();

    if (nextLevelText != null)
        nextLevelText.text = (level + 1).ToString();
}
}