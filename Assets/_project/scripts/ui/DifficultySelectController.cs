using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DifficultySelectController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName     = "SampleScene";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    [Header("Fade Overlay")]
    [SerializeField] private Image screenOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Difficulty Panels -- Easy (0), Medium (1), Hard (2)")]
    [SerializeField] private DifficultyPanel[] panels = new DifficultyPanel[3];

    [Header("Tutorial Button")]
    [SerializeField] private Button reviewTutorialButton;

    [Header("Deployment Cutscene")]
    [SerializeField] private GameObject  mainUIRoot;      // parent holding the three panels + tutorial button — hidden during cutscene
    [SerializeField] private CanvasGroup cutsceneGroup;   // full-screen panel, alpha starts at 0
    [SerializeField] private TMP_Text    cutsceneText;    // typewriter output (monospace font recommended)
    [SerializeField] private Image       cutsceneFlash;   // white/cyan flash overlay, starts clear
    [SerializeField] private float       cutsceneTypeDelay   = 0.022f;
    [SerializeField] private float       cutsceneFlickerSpeed = 10f;
    [SerializeField] private float       cutsceneFlickerAmt   = 0.04f;

    [System.Serializable]
    public sealed class DifficultyPanel
    {
        public Button     selectButton;
        public TMP_Text   nameText;
        public TMP_Text   descriptionText;
        public TMP_Text   bestRunText;
        public GameObject lockedOverlay;
        public TMP_Text   unlockHintText;
    }

    // ── Static data ────────────────────────────────────────────────────────

    private static readonly string[] DifficultyNames =
        { "EASY", "MEDIUM", "HARD" };

    private static readonly string[] DifficultyDescriptions =
    {
        "Standard enemies.\nGood for learning the ropes.",
        "Enemies have 50% more health\nand move 15% faster.",
        "Enemies have 120% more health\nand move 35% faster.\nNo mercy.",
    };

    private static readonly string[] UnlockHints =
    {
        "",
        "CLEAR WAVE 10 ON EASY TO UNLOCK",
        "CLEAR WAVE 10 ON MEDIUM TO UNLOCK",
    };

    // Per-difficulty deployment briefing lines
    private static readonly string[][] DeploymentScript =
    {
        // ── EASY ──────────────────────────────────────────────────────────
        new[]
        {
            "OPERATIVE DEPLOYMENT CONFIRMED",
            "--------------------------------",
            "",
            "THREAT LEVEL :  STANDARD",
            "SECTOR       :  7-G",
            "ENEMY STATUS :  ACTIVE",
            "",
            "[SYS]  Arming weapons.................OK",
            "[SYS]  Calibrating neural link........OK",
            "[SYS]  Deploying operative............OK",
            "",
            "You know what to do.",
            "Good luck.",
        },

        // ── MEDIUM ────────────────────────────────────────────────────────
        new[]
        {
            "OPERATIVE DEPLOYMENT CONFIRMED",
            "--------------------------------",
            "",
            "THREAT LEVEL :  ELEVATED",
            "SECTOR       :  7-G",
            "ENEMY STATUS :  HIGH DENSITY",
            "",
            "[SYS]  Arming weapons.................OK",
            "[SYS]  Calibrating neural link........OK",
            "[SYS]  Deploying operative............OK",
            "[SYS]  WARNING: Threat escalation detected.",
            "",
            "They hit harder here.",
            "Proceed with caution.",
        },

        // ── HARD ──────────────────────────────────────────────────────────
        new[]
        {
            "OPERATIVE DEPLOYMENT CONFIRMED",
            "--------------------------------",
            "",
            "THREAT LEVEL :  CRITICAL",
            "SECTOR       :  7-G",
            "ENEMY STATUS :  OVERWHELMING",
            "",
            "[SYS]  Arming weapons.................OK",
            "[SYS]  Calibrating neural link........OK",
            "[SYS]  Deploying operative............OK",
            "[SYS]  WARNING: Extreme threat detected.",
            "[SYS]  ERROR  : Survival probability LOW.",
            "",
            "You asked for this.",
            "We cannot guarantee your return.",
            "",
            "Good luck, operative.",
            "You are going to need it.",
        },
    };

    // ── Runtime state ──────────────────────────────────────────────────────

    private bool cutsceneSkipPressed = false;
    private bool inCutscene          = false;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Start()
    {
        if (cutsceneGroup != null) { cutsceneGroup.alpha = 0f; cutsceneGroup.gameObject.SetActive(false); }
        if (cutsceneFlash != null) { Color c = cutsceneFlash.color; c.a = 0f; cutsceneFlash.color = c; }

        StartCoroutine(FadeIn());
        RefreshPanels();

        if (reviewTutorialButton != null)
            reviewTutorialButton.onClick.AddListener(OnReviewTutorial);
    }

    private void Update()
    {
        // CRT flicker on cutscene text
        if (inCutscene && cutsceneText != null)
        {
            float noise   = Mathf.PerlinNoise(Time.time * cutsceneFlickerSpeed, 0f);
            float flicker = 1f - cutsceneFlickerAmt * (noise - 0.5f) * 2f;
            Color c = cutsceneText.color;
            cutsceneText.color = new Color(c.r * flicker, c.g * flicker, c.b * flicker, c.a);
        }

        // Skip input during cutscene
        if (inCutscene)
        {
            bool pressed =
                (Keyboard.current != null &&
                 (Keyboard.current.spaceKey.wasPressedThisFrame ||
                  Keyboard.current.enterKey.wasPressedThisFrame)) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if (pressed) cutsceneSkipPressed = true;
        }
    }

    // ── Panel population ───────────────────────────────────────────────────

    private void RefreshPanels()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            DifficultyPanel p = panels[i];
            if (p == null) continue;

            bool unlocked = GameProgress.Instance != null
                ? GameProgress.Instance.IsDifficultyUnlocked(i)
                : i == 0;

            if (p.nameText        != null) p.nameText.text        = DifficultyNames[i];
            if (p.descriptionText != null) p.descriptionText.text = DifficultyDescriptions[i];
            if (p.lockedOverlay   != null) p.lockedOverlay.SetActive(!unlocked);
            if (p.unlockHintText  != null)
            {
                p.unlockHintText.gameObject.SetActive(!unlocked);
                p.unlockHintText.text = UnlockHints[i];
            }
            if (p.selectButton != null) p.selectButton.interactable = unlocked;

            if (p.bestRunText != null)
            {
                if (GameProgress.Instance != null)
                {
                    var (wave, gun, time) = GameProgress.Instance.GetBestRun(i);
                    if (wave > 0)
                    {
                        int mins = Mathf.FloorToInt(time / 60f);
                        int secs = Mathf.FloorToInt(time % 60f);
                        p.bestRunText.text = $"BEST: WAVE {wave}  |  {gun}  |  {mins:00}:{secs:00}";
                    }
                    else
                    {
                        p.bestRunText.text = "NO RUN RECORDED";
                    }
                }
                else
                {
                    p.bestRunText.text = "NO RUN RECORDED";
                }
            }

            int captured = i;
            if (p.selectButton != null)
            {
                p.selectButton.onClick.RemoveAllListeners();
                p.selectButton.onClick.AddListener(() => OnDifficultySelected(captured));
            }
        }
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private void OnDifficultySelected(int difficulty)
    {
        GameProgress.SelectedDifficulty = difficulty;
        SetButtonsInteractable(false);
        StartCoroutine(DeploymentCutscene(difficulty));
    }

    private void OnReviewTutorial()
    {
        GameProgress.ReviewingTutorial = true;
        StartCoroutine(LoadSceneWithFade(tutorialSceneName));
    }

    // ── Deployment cutscene ────────────────────────────────────────────────

    private IEnumerator DeploymentCutscene(int difficulty)
    {
        inCutscene          = true;
        cutsceneSkipPressed = false;

        // Fade main UI to black, then hide it so it doesn't bleed through
        yield return FadeOut();
        if (mainUIRoot != null) mainUIRoot.SetActive(false);

        // Show cutscene panel
        if (cutsceneGroup != null)
        {
            cutsceneGroup.gameObject.SetActive(true);
            cutsceneGroup.alpha = 1f;
        }
        if (cutsceneText != null)
        {
            cutsceneText.text  = "";
            cutsceneText.color = new Color(0f, 0.95f, 1f, 1f);
        }
        if (screenOverlay != null)
        {
            Color c = screenOverlay.color; c.a = 0f; screenOverlay.color = c;
        }

        yield return new WaitForSeconds(0.3f);

        // Type out lines
        string[] lines  = DeploymentScript[Mathf.Clamp(difficulty, 0, DeploymentScript.Length - 1)];
        string   buffer = "";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrEmpty(line))
            {
                buffer += "\n";
                if (cutsceneText != null) cutsceneText.text = buffer;
                yield return CutsceneWait(0.18f);
                continue;
            }

            // Separator lines appear instantly
            bool instant = line.StartsWith("---") || line.StartsWith("===");
            float delay  = instant ? 0f : cutsceneTypeDelay;

            if (cutsceneSkipPressed)
            {
                // Dump all remaining lines at once
                for (int j = i; j < lines.Length; j++)
                    buffer += lines[j] + "\n";
                if (cutsceneText != null) cutsceneText.text = buffer;
                break;
            }

            foreach (char ch in line)
            {
                buffer += ch;
                string cursor = (Time.time % 0.5f < 0.25f) ? "_" : "";
                if (cutsceneText != null)
                    cutsceneText.text = buffer + cursor;

                if (!cutsceneSkipPressed && delay > 0f)
                    yield return new WaitForSeconds(delay);
            }

            buffer += "\n";
            if (cutsceneText != null) cutsceneText.text = buffer;

            // Longer pause after key lines
            bool isKeyLine = line.StartsWith("Good") || line.StartsWith("You") || line.StartsWith("Proceed");
            float postDelay = isKeyLine ? 0.55f : 0.10f;
            yield return CutsceneWait(postDelay);
        }

        // Hold at end
        yield return CutsceneWait(1.0f);

        // Flash — cyan for easy, white-ish for medium, red-tinged for hard
        Color[] flashColors =
        {
            new Color(0f,   0.9f, 1f,   1f),   // cyan
            new Color(0.8f, 0.9f, 1f,   1f),   // cool white
            new Color(1f,   0.3f, 0.2f, 1f),   // red
        };

        yield return FlashScreen(flashColors[Mathf.Clamp(difficulty, 0, 2)]);

        // Fade to black and load game
        yield return FadeOut();
        inCutscene = false;
        SceneManager.LoadScene(gameSceneName);
    }

    // Waits for duration but can be cut short by skip input
    private IEnumerator CutsceneWait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !cutsceneSkipPressed)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FlashScreen(Color flashColor)
    {
        if (cutsceneFlash == null) yield break;

        // Punch in
        float elapsed = 0f, dur = 0.12f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            Color c = flashColor;
            c.a = Mathf.Lerp(0f, 1f, elapsed / dur);
            cutsceneFlash.color = c;
            yield return null;
        }

        // Hold one frame
        yield return null;

        // Fade out
        elapsed = 0f; dur = 0.55f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            Color c = flashColor;
            c.a = Mathf.Lerp(1f, 0f, elapsed / dur);
            cutsceneFlash.color = c;
            yield return null;
        }

        Color clear = cutsceneFlash.color; clear.a = 0f; cutsceneFlash.color = clear;
    }

    // ── Scene transitions ──────────────────────────────────────────────────

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        SetButtonsInteractable(false);
        yield return FadeOut();
        SceneManager.LoadScene(sceneName);
    }

    private void SetButtonsInteractable(bool on)
    {
        foreach (DifficultyPanel p in panels)
            if (p?.selectButton != null) p.selectButton.interactable = on;
        if (reviewTutorialButton != null) reviewTutorialButton.interactable = on;
    }

    // ── Fade helpers ───────────────────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        if (screenOverlay == null) yield break;
        float elapsed = 0f;
        Color c = Color.black; c.a = 1f; screenOverlay.color = c;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            screenOverlay.color = c;
            yield return null;
        }
        c.a = 0f; screenOverlay.color = c;
    }

    private IEnumerator FadeOut()
    {
        if (screenOverlay == null) yield break;
        float elapsed = 0f;
        Color c = screenOverlay.color;
        float startA = c.a;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startA, 1f, elapsed / fadeDuration);
            screenOverlay.color = c;
            yield return null;
        }
        c.a = 1f; screenOverlay.color = c;
    }
}
