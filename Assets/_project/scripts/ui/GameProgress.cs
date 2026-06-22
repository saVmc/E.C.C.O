using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    // 0 = Easy, 1 = Medium, 2 = Hard
    public static int SelectedDifficulty { get; set; } = 0;

    private const string KeyMediumUnlocked = "ecco_medium_unlocked";
    private const string KeyHardUnlocked   = "ecco_hard_unlocked";
    private const string KeyTutorialSeen   = "ecco_tutorial_seen";
    private const string KeyBestWave       = "ecco_best_wave_";
    private const string KeyBestGun        = "ecco_best_gun_";
    private const string KeyBestTime       = "ecco_best_time_";

    private float gameStartTime = 0f;
    public float GameStartTime => gameStartTime;

    public (int wave, string gun, float time) LastRunResult { get; private set; }

    public static bool ReviewingTutorial { get; set; } = false;

    // Auto-creates a GameProgress if one isn't already in the scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("GameProgress");
        go.AddComponent<GameProgress>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            gameStartTime = Time.time;
            PrestigeEffects.Reset();
            StartCoroutine(SubscribeToHordeSpawner());
            StartCoroutine(SubscribeToPlayerDeath());
        }
    }

    private IEnumerator SubscribeToHordeSpawner()
    {
        while (HordeSpawner.Instance == null) yield return null;
        HordeSpawner.Instance.OnWaveCompleted += HandleWaveCompleted;
    }

    private IEnumerator SubscribeToPlayerDeath()
    {
        while (PlayerHealth.Instance == null) yield return null;
        PlayerHealth.Instance.OnDeath += HandlePlayerDeath;
    }

    private void HandleWaveCompleted(int wave)
    {
        if (wave >= 10)
            UnlockNextDifficulty(SelectedDifficulty);
    }

    private void HandlePlayerDeath()
    {
        int wave = HordeSpawner.Instance != null ? HordeSpawner.HighestWaveCompleted : 0;

        string gunName = "N/A";
        PlayerShooter shooter = FindAnyObjectByType<PlayerShooter>();
        if (shooter != null)
        {
            Gun gun = shooter.GetActiveGun();
            if (gun != null && gun.CurrentProfile != null)
                gunName = gun.CurrentProfile.DisplayName;
        }

        float elapsed = Time.time - gameStartTime;
        LastRunResult = (wave, gunName, elapsed);
        SaveRunResult(wave, gunName, elapsed);
    }

    public bool IsDifficultyUnlocked(int difficulty)
    {
        return difficulty switch
        {
            0 => true,
            1 => PlayerPrefs.GetInt(KeyMediumUnlocked, 0) == 1,
            2 => PlayerPrefs.GetInt(KeyHardUnlocked, 0) == 1,
            _ => false
        };
    }

    private void UnlockNextDifficulty(int currentDifficulty)
    {
        if (currentDifficulty == 0 && PlayerPrefs.GetInt(KeyMediumUnlocked, 0) == 0)
        {
            PlayerPrefs.SetInt(KeyMediumUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("[GameProgress] Medium difficulty unlocked!");
        }
        else if (currentDifficulty == 1 && PlayerPrefs.GetInt(KeyHardUnlocked, 0) == 0)
        {
            PlayerPrefs.SetInt(KeyHardUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("[GameProgress] Hard difficulty unlocked!");
        }
    }

    public bool HasSeenTutorial
    {
        get => PlayerPrefs.GetInt(KeyTutorialSeen, 0) == 1;
        set { PlayerPrefs.SetInt(KeyTutorialSeen, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public void SaveRunResult(int wave, string gunName, float timePlayed)
    {
        int diff = SelectedDifficulty;
        int prevBest = PlayerPrefs.GetInt(KeyBestWave + diff, 0);
        if (wave > prevBest)
        {
            PlayerPrefs.SetInt(KeyBestWave + diff, wave);
            PlayerPrefs.SetString(KeyBestGun + diff, string.IsNullOrEmpty(gunName) ? "N/A" : gunName);
            PlayerPrefs.SetFloat(KeyBestTime + diff, timePlayed);
            PlayerPrefs.Save();
        }
    }

    public (int wave, string gun, float time) GetBestRun(int difficulty)
    {
        int    wave = PlayerPrefs.GetInt(KeyBestWave + difficulty, 0);
        string gun  = PlayerPrefs.GetString(KeyBestGun + difficulty, "N/A");
        float  time = PlayerPrefs.GetFloat(KeyBestTime + difficulty, 0f);
        return (wave, gun, time);
    }

    [ContextMenu("DEBUG - Reset All Progress")]
    public void DEBUG_ResetAll()
    {
        PlayerPrefs.DeleteKey(KeyMediumUnlocked);
        PlayerPrefs.DeleteKey(KeyHardUnlocked);
        PlayerPrefs.DeleteKey(KeyTutorialSeen);
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.DeleteKey(KeyBestWave + i);
            PlayerPrefs.DeleteKey(KeyBestGun + i);
            PlayerPrefs.DeleteKey(KeyBestTime + i);
        }
        PlayerPrefs.Save();
        Debug.Log("[GameProgress] All progress reset.");
    }

    // Static helpers — safe to call even without an Instance
    public static float EnemyHealthMultiplier => SelectedDifficulty switch
    {
        0 => 1f,
        1 => 1.5f,
        2 => 2.2f,
        _ => 1f
    };

    public static float EnemySpeedMultiplier => SelectedDifficulty switch
    {
        0 => 1f,
        1 => 1.15f,
        2 => 1.35f,
        _ => 1f
    };
}
