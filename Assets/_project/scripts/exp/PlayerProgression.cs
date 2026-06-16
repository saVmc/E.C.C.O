using System;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("EXP Curve")]
    [SerializeField] private int baseExp = 80;
    [SerializeField] private float levelExponent = 2.1f;

    private int currentLevel = 1;
    private int currentExp = 0;
    private int expToNextLevel;

    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUp;

    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        expToNextLevel = CalculateExpRequired(currentLevel);
    }

    private int CalculateExpRequired(int level)
    {
        return Mathf.RoundToInt(baseExp * Mathf.Pow(level, levelExponent));
    }

    public void AddExp(int amount)
    {
        Debug.Log($"AddExp called: +{amount} | Total: {currentExp + amount}");
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            currentLevel++;
            expToNextLevel = CalculateExpRequired(currentLevel);
            OnLevelUp?.Invoke(currentLevel);
        }
    }
}