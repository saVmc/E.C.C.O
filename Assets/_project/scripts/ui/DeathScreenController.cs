using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Auto-creates itself — no prefab, no Inspector wiring needed.
/// Place nothing in the scene. It subscribes to ScreenFX.OnDeathScreenReady and
/// builds its own Canvas at sort-order 9999 so it is always on top.
///
/// Optional: drop a DeathScreenSettings component anywhere in SampleScene and assign
/// your imported font there — the controller will pick it up.
/// </summary>
public sealed class DeathScreenController : MonoBehaviour
{
    public static DeathScreenController Instance { get; private set; }

    // Optionally set from DeathScreenSettings in the scene
    public static TMP_FontAsset OverrideFont { get; set; }

    // ── Config ────────────────────────────────────────────────────────────
    private static readonly Color TextColor    = new Color(0f, 0.95f, 1f, 1f);
    private const float CharDelay    = 0.022f;
    private const float FlickerSpeed = 8f;
    private const float FlickerAmt   = 0.035f;
    private const float FontSize     = 45f;
    private const string RestartScene = "SampleScene";
    private const string MenuScene    = "DifficultySelect";

    // ── Runtime UI ────────────────────────────────────────────────────────
    private Canvas   canvas;
    private TMP_Text terminalText;

    // ── State ─────────────────────────────────────────────────────────────
    private bool waitForInput   = false;
    private bool restartPressed = false;
    private bool menuPressed    = false;
    private bool isShowing      = false;

    // ── Bootstrap ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("[DeathScreen]");
        go.AddComponent<DeathScreenController>(); // Awake() handles everything
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ScreenFX.OnDeathScreenReady += OnDeathScreenReady;
        Debug.Log("[DeathScreen] Awake — subscribed to ScreenFX.OnDeathScreenReady");
    }

    private void OnDestroy()
    {
        ScreenFX.OnDeathScreenReady -= OnDeathScreenReady;
    }

    // ── Trigger ───────────────────────────────────────────────────────────

    private void OnDeathScreenReady()
    {
        if (isShowing) return;
        isShowing = true;
        Debug.Log("[DeathScreen] OnDeathScreenReady fired — building UI");

        BuildUI();

        canvas.gameObject.SetActive(true);

        restartPressed = false;
        menuPressed    = false;
        waitForInput   = false;

        var result = GameProgress.Instance != null
            ? GameProgress.Instance.LastRunResult
            : (wave: 0, gun: "N/A", time: 0f);

        StartCoroutine(RunSequence(result.wave, result.gun, result.time));
    }

    // ── Input ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!waitForInput) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
                restartPressed = true;

            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.tabKey.wasPressedThisFrame)
                menuPressed = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            restartPressed = true;
    }

    // ── Sequence ──────────────────────────────────────────────────────────

    private IEnumerator RunSequence(int wave, string gunName, float elapsedSeconds)
    {
        Debug.Log("[DeathScreen] RunSequence starting");
        string timeStr = FormatTime(elapsedSeconds);

        yield return new WaitForSecondsRealtime(0.4f);

        var lines = new (string text, float cd, float post)[]
        {
            ("",                                                         0,         0.06f),
            ("> SIGNAL LOST",                                            CharDelay, 0.06f),
            ("> ─────────────────────────────────────",                  0.004f,    0.22f),
            ("",                                                         0,         0.06f),
            ("> OPERATIVE STATUS:    TERMINATED",                        CharDelay, 0.10f),
            ("",                                                         0,         0.06f),
            ($"> WAVE REACHED:        {wave}",                           CharDelay, 0.08f),
            ($"> WEAPON DEPLOYED:     {gunName}",                        CharDelay, 0.08f),
            ($"> TIME SURVIVED:       {timeStr}",                        CharDelay, 0.25f),
            ("",                                                         0,         0.06f),
            ("> ─────────────────────────────────────",                  0.004f,    0.12f),
            ("",                                                         0,         0.06f),
            ("> UPLOADING COMBAT DATA TO E.C.C.O DATABASE...",           CharDelay, 0.06f),
            ("> BACKUP COMPLETE.",                                        0.018f,   0.28f),
            ("",                                                         0,         0.06f),
            ("> MISSION STATUS:      FAILED",                            CharDelay, 0.45f),
            ("",                                                         0,         0.12f),
        };

        var committed = new List<string>();

        foreach (var (text, cd, post) in lines)
        {
            if (string.IsNullOrEmpty(text))
            {
                committed.Add("");
                Render(committed, null);
                if (post > 0f) yield return new WaitForSecondsRealtime(post);
                continue;
            }

            string typed = "";
            foreach (char c in text)
            {
                typed += c;
                string cursor = (Time.unscaledTime % 0.55f < 0.28f) ? "_" : "";
                Render(committed, typed + cursor);
                Flicker();
                if (cd > 0f) yield return new WaitForSecondsRealtime(cd);
            }
            committed.Add(typed);
            Render(committed, null);
            Flicker();

            if (post > 0f) yield return new WaitForSecondsRealtime(post);
        }

        waitForInput = true;
        float t = 0f;
        while (!restartPressed && !menuPressed)
        {
            t += Time.unscaledDeltaTime;
            string cur = (t % 0.7f < 0.38f) ? "_" : " ";
            Render(committed, $"> [ ENTER / ESC  continue ]{cur}");
            Flicker();
            yield return null;
        }

        waitForInput = false;
        yield return StartCoroutine(FadeOutTerminal(0.8f));
        isShowing    = false;
        Time.timeScale = 1f;
        // Keep canvas (black BG) alive through the load so the game arena never flashes.
        // Hide it one frame after the new scene initialises its own overlay.
        SceneManager.sceneLoaded += HideCanvasOnLoad;
        SceneManager.LoadScene(MenuScene);
    }

    private void HideCanvasOnLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HideCanvasOnLoad;
        StartCoroutine(HideCanvasNextFrame());
    }

    private IEnumerator HideCanvasNextFrame()
    {
        yield return null; // one frame: lets DifficultySelect Start() run and set its own overlay
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutTerminal(float duration)
    {
        Color start = terminalText != null ? terminalText.color : TextColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
            if (terminalText != null)
                terminalText.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }
    }

    // ── UI helpers ────────────────────────────────────────────────────────

    private void Render(List<string> committed, string inProgress)
    {
        if (terminalText == null) return;
        var sb = new StringBuilder();
        foreach (string l in committed) { sb.Append(l); sb.Append('\n'); }
        if (inProgress != null) sb.Append(inProgress);
        terminalText.text = sb.ToString();
    }

    private void Flicker()
    {
        if (terminalText == null) return;
        float n = Mathf.PerlinNoise(Time.unscaledTime * FlickerSpeed, 0f);
        float f = 1f - FlickerAmt * (n - 0.5f) * 2f;
        Color c = TextColor;
        c.r *= f; c.g *= f; c.b *= f;
        terminalText.color = c;
    }

    private static string FormatTime(float s)
    {
        int m = Mathf.FloorToInt(s / 60f);
        int sec = Mathf.FloorToInt(s % 60f);
        return $"{m:D2}:{sec:D2}";
    }

    // ── Procedural Canvas build (called once on first death) ──────────────

    private void BuildUI()
    {
        if (canvas != null) return;

        var canvasGO = new GameObject("_DeathCanvas");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Solid black background — covers everything beneath
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        var bgRT  = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Terminal text
        var textGO = new GameObject("TerminalText");
        textGO.transform.SetParent(canvasGO.transform, false);
        terminalText = textGO.AddComponent<TextMeshProUGUI>();
        terminalText.color     = TextColor;
        terminalText.fontSize  = FontSize;
        terminalText.alignment = TextAlignmentOptions.TopLeft;
        terminalText.text      = "";

        // Font: use static override first, then TMP default
        if (OverrideFont != null)
            terminalText.font = OverrideFont;

        var textRT = terminalText.rectTransform;
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(110, 80);
        textRT.offsetMax = new Vector2(-110, -80);

        canvasGO.SetActive(false);
        Debug.Log("[DeathScreen] Canvas built at sort order 9999");
    }
}
