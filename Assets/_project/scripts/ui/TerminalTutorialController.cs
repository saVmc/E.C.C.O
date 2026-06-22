using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TerminalTutorialController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text terminalText;
    [SerializeField] private Image    scanlineOverlay;
    [SerializeField] private Image    screenOverlay;
    [SerializeField] private TMP_Text skipHintText;

    [Header("Logo (assign a monospace font TMP_Text -- use unifont-17.0.04 SDF)")]
    [SerializeField] private TMP_Text logoText;
    [SerializeField] private float    logoHoldDuration    = 1.8f;
    [SerializeField] private float    logoFadeOutDuration = 0.5f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName  = "DifficultySelect";
    [Tooltip("Uncheck to always play the tutorial regardless of save state (useful in Editor)")]
    [SerializeField] private bool   skipIfSeen     = true;

    [Header("Display")]
    [SerializeField] private int   maxVisibleLines = 14;
    [SerializeField] private float fadeDuration    = 0.55f;

    [Header("CRT Style")]
    [SerializeField] private Color textColor     = new Color(0f, 0.95f, 1f, 1f);
    [SerializeField] private float scanlineAlpha = 0.10f;
    [SerializeField] private float flickerSpeed  = 9f;
    [SerializeField] private float flickerAmount = 0.03f;

    // ── Line definition ────────────────────────────────────────────────────

    private struct TLine
    {
        public string text;
        public float  charDelay;
        public float  preDelay;
        public float  postDelay;
        public bool   instant;
        public bool   waitForInput;   // pause here and show [ ENTER to continue ]

        public TLine(string t, float cd = 0.026f, float pre = 0f, float post = 0.10f, bool ins = false)
        {
            text = t; charDelay = cd; preDelay = pre; postDelay = post;
            instant = ins; waitForInput = false;
        }

        // Factory for section-break sentinels
        public static TLine Break => new TLine("") { waitForInput = true };
    }

    // ── Script ─────────────────────────────────────────────────────────────

    // Logo content rendered in logoText (separate TMP with monospace font)
    private static readonly string LogoArt =
        "$$$$$$$$\\        $$$$$$\\         $$$$$$\\      $$$$$$\\  \n" +
        "$$  _____|      $$  __$$\\       $$  __$$\\    $$  __$$\\ \n" +
        "$$ |            $$ /  \\__|      $$ /  \\__|   $$ /  $$ |\n" +
        "$$$$$\\          $$ |            $$ |         $$ |  $$ |\n" +
        "$$  __|         $$ |            $$ |         $$ |  $$ |\n" +
        "$$ |            $$ |  $$\\       $$ |  $$\\    $$ |  $$ |\n" +
        "$$$$$$$$\\  $$\\  \\$$$$$$  | $$\\  \\$$$$$$  |$$\\ $$$$$$  |\n" +
        "\\________| \\__|  \\______/  \\__|  \\______/ \\__|\\______/ \n" +
        "\n" +
        "  E N T I T Y   C O N T A I N M E N T   C O N T R O L   O P E R A T I O N S";

    private static readonly TLine[] Script =
    {
        // ════════════════════════════════════════
        // BOOT SEQUENCE  (auto-plays, no wait)
        // ════════════════════════════════════════

        new TLine("ECCO SYSTEMS BIOS v2.1.0",                 0.025f, 0.00f, 0.08f),
        new TLine("Copyright (C) 2026 E.C.C.O Corp",          0.020f, 0.00f, 0.30f),
        new TLine("",                                          0,      0,     0.06f, true),
        new TLine("Checking CPU.......Intel Core i5-6500  OK", 0.012f, 0.00f, 0.05f),
        new TLine("Checking RAM...........8192 MB         OK", 0.012f, 0.00f, 0.05f),
        new TLine("Checking GPU............NVIDIA GT 730  OK", 0.012f, 0.00f, 0.05f),
        new TLine("Checking NET...........Uplink active   OK", 0.012f, 0.00f, 0.20f),
        new TLine("",                                          0,      0,     0.05f, true),
        new TLine("Loading kernel image...................",     0.010f, 0.00f, 0.00f),
        new TLine(" done.",                                    0.035f, 0.00f, 0.15f),
        new TLine("Mounting secure partitions............",     0.010f, 0.00f, 0.00f),
        new TLine(" done.",                                    0.035f, 0.00f, 0.15f),
        new TLine("Initialising operative interface......",     0.010f, 0.00f, 0.00f),
        new TLine(" done.",                                    0.035f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.05f, true),
        new TLine("[  0.000] ECCO kernel v4.7.2 initialised",  0.010f, 0.00f, 0.04f),
        new TLine("[  0.238] Neural link established",          0.010f, 0.00f, 0.04f),
        new TLine("[  0.617] Threat matrix loaded",             0.010f, 0.00f, 0.55f),
        new TLine("[  1.204] Operative profile: UNKNOWN",       0.010f, 0.00f, 0.08f),
        new TLine("[  1.205] Clearance level: ALPHA",           0.010f, 0.00f, 0.08f),
        new TLine("[  1.412] Mission status: ACTIVE",           0.010f, 0.00f, 0.55f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,   // <-- waits for ENTER

        // ════════════════════════════════════════
        // SITUATION REPORT
        // ════════════════════════════════════════

        new TLine("SITUATION REPORT",                          0.035f, 0.00f, 0.20f),
        new TLine("----------------",                          0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.05f, true),
        new TLine("Location:       SECTOR 7-G",                0.022f, 0.00f, 0.08f),
        new TLine("Threat level:   CRITICAL",                  0.022f, 0.00f, 0.08f),
        new TLine("Enemy contact:  CONFIRMED",                 0.022f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.12f, true),
        new TLine("Our systems have been breached.",           0.030f, 0.00f, 0.15f),
        new TLine("An unidentified horde is converging",       0.030f, 0.00f, 0.10f),
        new TLine("on your position.",                         0.030f, 0.00f, 0.45f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("You are the last line of defence.",         0.030f, 0.00f, 0.20f),
        new TLine("Eliminate all hostiles.",                   0.030f, 0.00f, 0.12f),
        new TLine("Do not let them get through you.",           0.030f, 0.00f, 0.55f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // MODULE 01: MOVEMENT
        // ════════════════════════════════════════

        new TLine("MODULE 01: MOVEMENT",                       0.032f, 0.00f, 0.20f),
        new TLine("-------------------",                       0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("  W / A / S / D     Move",                  0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Arrow keys        Move  (alternate)",    0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Left Shift        Sprint  (+50% speed)", 0.022f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("NOTE: Your weapon auto-hides while",        0.025f, 0.00f, 0.10f),
        new TLine("moving. It reappears the instant you",      0.025f, 0.00f, 0.10f),
        new TLine("stop or open fire.",                        0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // MODULE 02: COMBAT
        // ════════════════════════════════════════

        new TLine("MODULE 02: COMBAT",                         0.032f, 0.00f, 0.20f),
        new TLine("-----------------",                         0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("  Left Click       Fire",                  0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Space            Fire  (alternate)",     0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  R                Manual reload",         0.022f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Aim with your mouse cursor.",               0.025f, 0.00f, 0.10f),
        new TLine("Press R to reload your weapon.",                  0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Different weapons have different fire",     0.025f, 0.00f, 0.10f),
        new TLine("rates, ranges, and magazine sizes.",        0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // MODULE 03: ABILITIES
        // ════════════════════════════════════════

        new TLine("MODULE 03: ABILITIES",                      0.032f, 0.00f, 0.20f),
        new TLine("--------------------",                      0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("  Q                Ability slot 1",        0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  E                Ability slot 2",        0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  C                Ability slot 3",        0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  F                Ability slot 4",        0.022f, 0.00f, 0.10f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Tab              Pause / ability info",  0.022f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Abilities recharge over time.",             0.025f, 0.00f, 0.10f),
        new TLine("The hotbar icon dims while recharging.",    0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Up to 4 abilities can be equipped.",        0.025f, 0.00f, 0.10f),
        new TLine("Unlock and upgrade them between waves.",    0.025f, 0.00f, 0.10f),
        new TLine("Each ability has 5 upgrade tiers.",         0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // MODULE 04: PROGRESSION
        // ════════════════════════════════════════

        new TLine("MODULE 04: PROGRESSION",                    0.032f, 0.00f, 0.20f),
        new TLine("----------------------",                    0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("Defeated enemies drop EXP orbs.",           0.025f, 0.00f, 0.10f),
        new TLine("Walk over orbs to collect them.",           0.025f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Fill the EXP bar to LEVEL UP.",             0.025f, 0.00f, 0.10f),
        new TLine("A card selection screen will appear.",      0.025f, 0.00f, 0.10f),
        new TLine("Choose one of three offered cards:",        0.025f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("  - New ability",                          0.022f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  - Ability upgrade  (up to 5 stars)",     0.022f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  - New weapon",                           0.022f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  - Weapon upgrade",                       0.022f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("You can only carry one weapon at a time.",  0.025f, 0.00f, 0.10f),
        new TLine("One per infiltration mission, that is.",                   0.025f, 0.00f, 0.40f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // MODULE 05: WAVES
        // ════════════════════════════════════════

        new TLine("MODULE 05: WAVES",                          0.032f, 0.00f, 0.20f),
        new TLine("-----------------",                         0.008f, 0.00f, 0.25f),
        new TLine("",                                          0,      0,     0.08f, true),
        new TLine("Hostile entities manifest as geometric",    0.025f, 0.00f, 0.10f),
        new TLine("shapes. Do not underestimate them.",        0.025f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Enemies arrive in numbered waves.",         0.025f, 0.00f, 0.10f),
        new TLine("Clear all hostiles to end the wave.",       0.025f, 0.00f, 0.10f),
        new TLine("A rest period follows between waves.",      0.025f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Wave  5, 10, 15...  Boss encounter",     0.018f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Wave  6            [CLASSIFIED GUN]",        0.018f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Wave 10  (Easy)    Unlocks MEDIUM",      0.018f, 0.00f, 0.08f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("  Wave 10  (Medium)  Unlocks HARD",        0.018f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.10f, true),
        new TLine("Enemies scale in health and speed",         0.025f, 0.00f, 0.10f),
        new TLine("with every level you gain.",                0.025f, 0.00f, 0.10f),
        new TLine("There is no final wave. Survive.",          0.025f, 0.00f, 0.50f),
        new TLine("",                                          0,      0,     0.10f, true),

        TLine.Break,

        // ════════════════════════════════════════
        // SIGN-OFF
        // ════════════════════════════════════════

        new TLine("MISSION BRIEFING COMPLETE",                 0.030f, 0.00f, 0.20f),
        new TLine("-------------------------",                 0.008f, 0.00f, 0.35f),
        new TLine("",                                          0,      0,     0.12f, true),
        new TLine("All systems nominal.",                      0.030f, 0.00f, 0.20f),
        new TLine("If you must leave, death is your only option.",                     0.030f, 0.00f, 0.20f),
        new TLine("Good luck, operative.",                     0.030f, 0.00f, 0.20f),
        new TLine("",                                          0,      0,     0.15f, true),
        new TLine("You're going to need it.",                  0.035f, 0.00f, 0.60f),
        new TLine("",                                          0,      0,     0.20f, true),
    };

    // ── State ─────────────────────────────────────────────────────────────

    private readonly List<string> allLines = new List<string>();
    private int  scrollOffset     = 0;   // 0 = at bottom; positive = scrolled back
    private bool skipLine         = false;
    private bool atSectionBreak   = false;
    private bool advanceSection   = false;
    private bool atEnd            = false;
    private bool proceedNow       = false;
    private bool continueLine     = false;  // next line appends to current (no newline)

    // ── Unity lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        bool alreadySeen = GameProgress.Instance != null && GameProgress.Instance.HasSeenTutorial;
        bool reviewing   = GameProgress.ReviewingTutorial;
        GameProgress.ReviewingTutorial = false;

        if (skipIfSeen && alreadySeen && !reviewing)
        {
            if (screenOverlay != null) screenOverlay.color = Color.black;
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (terminalText != null)  { terminalText.color = textColor; terminalText.text = ""; }
        if (screenOverlay != null) { screenOverlay.color = Color.black; screenOverlay.raycastTarget = false; }
        if (scanlineOverlay != null)
        {
            Color sc = scanlineOverlay.color; sc.a = scanlineAlpha; scanlineOverlay.color = sc;
            scanlineOverlay.raycastTarget = false;
        }
        if (skipHintText != null) skipHintText.gameObject.SetActive(false);

        if (logoText != null)
        {
            logoText.text  = LogoArt;
            logoText.alpha = 0f;
            logoText.gameObject.SetActive(true);
        }
    }

    private void Start() => StartCoroutine(RunSequence());

    private void Update()
    {
        ApplyCRTFlicker();
        HandleScrollInput();

        bool pressed = IsAnyConfirmKey();

        if (atSectionBreak)
        {
            if (pressed) advanceSection = true;
        }
        else if (atEnd)
        {
            if (pressed) proceedNow = true;
        }
        else
        {
            if (pressed) skipLine = true;
        }
    }

    // ── Input helpers ─────────────────────────────────────────────────────

    private bool IsAnyConfirmKey()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    private void HandleScrollInput()
    {
        int delta = 0;

        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (wheel >  0.1f) delta++;
            else if (wheel < -0.1f) delta--;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)   delta++;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame) delta--;
        }

        if (delta != 0)
        {
            int maxScroll = Mathf.Max(0, allLines.Count - maxVisibleLines);
            scrollOffset = Mathf.Clamp(scrollOffset + delta, 0, maxScroll);
            RenderLines();
        }
    }

    // ── Master sequence ───────────────────────────────────────────────────

    private IEnumerator RunSequence()
    {
        yield return FadeOverlay(1f, 0f, fadeDuration);

        // ── Logo splash on logoText (monospace TMP) ───────────────────────
        if (logoText != null)
        {
            float t = 0f;
            while (t < fadeDuration) { t += Time.deltaTime; logoText.alpha = Mathf.Clamp01(t / fadeDuration); yield return null; }
            logoText.alpha = 1f;
            yield return new WaitForSeconds(logoHoldDuration);
            t = 0f;
            while (t < logoFadeOutDuration) { t += Time.deltaTime; logoText.alpha = Mathf.Clamp01(1f - t / logoFadeOutDuration); yield return null; }
            logoText.alpha = 0f;
            logoText.gameObject.SetActive(false);
        }

        if (skipHintText != null)
        {
            skipHintText.text = "ENTER -- continue";
            skipHintText.text = "UP/DOWN -- scroll";

            skipHintText.gameObject.SetActive(true);
        }

        yield return PlayScript();

        atEnd = true;
        if (skipHintText != null) skipHintText.gameObject.SetActive(false);
        yield return BlinkWaitForInput("[ ENTER to deploy ]");

        if (GameProgress.Instance != null)
            GameProgress.Instance.HasSeenTutorial = true;

        yield return FadeOverlay(0f, 1f, fadeDuration);
        SceneManager.LoadScene(nextSceneName);
    }

    // ── Script playback ───────────────────────────────────────────────────

    private IEnumerator PlayScript()
    {
        for (int i = 0; i < Script.Length; i++)
        {
            TLine line = Script[i];

            // Section break sentinel
            if (line.waitForInput)
            {
                yield return SectionBreak();
                continue;
            }

            if (line.preDelay > 0f)
                yield return InterruptibleWait(line.preDelay);

            // Blank line
            if (string.IsNullOrEmpty(line.text))
            {
                allLines.Add("");
                RenderLines();
                if (line.postDelay > 0f)
                    yield return InterruptibleWait(line.postDelay);
                continueLine = false;
                continue;
            }

            // Continuation: append to last line (e.g. " done." after "Loading...")
            bool append = continueLine && line.text.StartsWith(" ");

            if (line.instant)
            {
                if (append && allLines.Count > 0)
                    allLines[allLines.Count - 1] += line.text;
                else
                    allLines.Add(line.text);
                RenderLines();
            }
            else
            {
                yield return TypewriterLine(line.text, line.charDelay, append);
            }

            skipLine = false;
            if (line.postDelay > 0f)
                yield return InterruptibleWait(line.postDelay);

            continueLine = line.text.StartsWith(" ");
        }
    }

    private IEnumerator TypewriterLine(string text, float charDelay, bool append)
    {
        string baseText = (append && allLines.Count > 0) ? allLines[allLines.Count - 1] : "";
        string typed    = "";

        foreach (char c in text)
        {
            typed += c;
            string cursor = (Time.time % 0.55f < 0.28f) ? "_" : "";
            RenderLines(baseText + typed + cursor);

            if (!skipLine)
                yield return new WaitForSeconds(charDelay);
        }

        // Commit
        if (append && allLines.Count > 0)
            allLines[allLines.Count - 1] = baseText + typed;
        else
            allLines.Add(typed);

        RenderLines();
        skipLine = false;
    }

    private IEnumerator SectionBreak()
    {
        // Scroll to bottom so prompt is visible
        scrollOffset  = 0;
        atSectionBreak = true;
        advanceSection = false;

        float t = 0f;
        while (!advanceSection)
        {
            t += Time.deltaTime;
            string cursor = (t % 0.7f < 0.35f) ? "_" : " ";
            RenderLines("[ ENTER to continue ]" + cursor);
            yield return null;
        }

        atSectionBreak = false;
        advanceSection = false;
        scrollOffset   = 0;

        // Blank spacer before next section
        allLines.Add("");
        allLines.Add("");
        RenderLines();
        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator BlinkWaitForInput(string prompt)
    {
        scrollOffset = 0;
        float t = 0f;
        while (!proceedNow)
        {
            t += Time.deltaTime;
            string cursor = (t % 0.7f < 0.35f) ? "_" : " ";
            RenderLines(prompt + cursor);
            yield return null;
        }
        RenderLines();
    }

    private IEnumerator InterruptibleWait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !skipLine)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        skipLine = false;
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    // Renders a sliding window of committed lines plus an optional in-progress line.
    // scrollOffset=0 shows the latest lines; positive values scroll back through history.
    private void RenderLines(string inProgressLine = null)
    {
        if (terminalText == null) return;

        int maxScroll = Mathf.Max(0, allLines.Count - maxVisibleLines);
        scrollOffset  = Mathf.Clamp(scrollOffset, 0, maxScroll);

        // Window of committed lines to display
        int endIdx   = allLines.Count - scrollOffset;
        int startIdx = Mathf.Max(0, endIdx - maxVisibleLines);

        var sb = new StringBuilder();

        // Top scroll indicator
        if (startIdx > 0)
            sb.AppendLine("  [...]");

        for (int i = startIdx; i < endIdx; i++)
        {
            if (i > startIdx) sb.Append('\n');
            sb.Append(allLines[i]);
        }

        // In-progress line (only visible when at the bottom)
        if (inProgressLine != null && scrollOffset == 0)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(inProgressLine);
        }

        // Bottom scroll indicator when scrolled up
        if (scrollOffset > 0)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("  [v v v  scroll down for latest  v v v]");
        }

        terminalText.text = sb.ToString();
    }

    // ── CRT flicker ───────────────────────────────────────────────────────

    private void ApplyCRTFlicker()
    {
        if (terminalText == null) return;
        float noise   = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float flicker = 1f - flickerAmount * (noise - 0.5f) * 2f;
        Color c = textColor;
        c.r *= flicker; c.g *= flicker; c.b *= flicker;
        terminalText.color = c;
    }

    // ── Fade overlay ──────────────────────────────────────────────────────

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (screenOverlay == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = screenOverlay.color;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            screenOverlay.color = c;
            yield return null;
        }
        Color f = screenOverlay.color; f.a = to; screenOverlay.color = f;
    }
}
