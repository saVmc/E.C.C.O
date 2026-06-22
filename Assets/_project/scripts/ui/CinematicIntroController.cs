using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CinematicIntroController : MonoBehaviour
{
    // ── Audio ──────────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private float beatDropTime = 23f;

    // ── Intro Layers ───────────────────────────────────────────────────────
    [Header("Intro Layers")]
    [SerializeField] private CanvasGroup introGroup;
    [SerializeField] private Image screenBackdrop;
    [SerializeField] private Image logoMain;
    [SerializeField] private Image logoGhostCyan;
    [SerializeField] private Image logoGhostMagenta;
    [SerializeField] private RectTransform logoStackRect;
    [SerializeField] private Image titlePlate;
    [SerializeField] private RectTransform titlePlateRect;
    [SerializeField] private Image titleArtwork;
    [SerializeField] private Sprite menuBackgroundSprite;
    [SerializeField] private Image menuBackdropImage;
    [SerializeField] private Image scanlineOverlay;   // optional — CRT scanlines

    // ── Menu ───────────────────────────────────────────────────────────────
    [Header("Menu")]
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private Button[] menuButtons;
    [SerializeField] private NeonUIButtonAnimator[] neonButtons;
    [SerializeField] private TMP_Text subtitleText;

    // ── Logo Sequence ──────────────────────────────────────────────────────
    [Header("Logo Sequence")]
    [SerializeField] private float emergenceDuration      = 1.5f;   // ghosts flicker in at far offsets
    [SerializeField] private float convergenceDuration    = 2.5f;   // ghosts drift + rush together
    [SerializeField] private float logoSettleDuration     = 1.2f;   // dims from overexposed to normal
    [SerializeField] private float logoFadeOutLength      = 1.5f;
    [SerializeField] private float convergenceStartOffset = 80f;    // canvas px — how far apart ghosts start
    [SerializeField] private float chromaticOffsetPixels  = 9f;

    // ── Title Reveal ───────────────────────────────────────────────────────
    [Header("Title Reveal")]
    [SerializeField] private float bgRevealDuration       = 0.85f;
    [SerializeField] private float plateWipeDuration      = 0.48f;
    [SerializeField] private float artworkResolveDuration = 0.9f;
    [SerializeField] private float subtitleEntranceLength = 0.72f;
    [SerializeField] private float titleHoldLength        = 2.3f;
    [SerializeField] private float menuRevealLength       = 1.0f;
    [SerializeField] private float bgAlpha                = 0.65f;  // background scene darkness
    [SerializeField] private float artworkCorruptPixels   = 18f;    // max jitter on artwork resolve
    [SerializeField] private float titlePulseAmount       = 0.04f;

    // ── Title Text ─────────────────────────────────────────────────────────
    [Header("Title Text")]
    [SerializeField] private float subtitlePulseAmount          = 0.012f;
    [SerializeField] private float subtitleGlitchChance         = 0.18f;
    [SerializeField] private float subtitleGlitchJitterPixels   = 4.5f;
    [SerializeField] private float subtitleChromaticShiftPixels = 5f;

    // ── Neon Pulse ─────────────────────────────────────────────────────────
    [Header("Neon Pulse")]
    [SerializeField] private float pulseBpm              = 82f;
    [SerializeField] private float pulseGlowAmount       = 0.032f;
    [SerializeField] private float pulseChromaticAmount  = 0.25f;
    [SerializeField] private float pulseEmissionAmount   = 0.08f;

    // ── Scene ──────────────────────────────────────────────────────────────
    [Header("Scene")]
    [SerializeField] private string tutorialSceneName      = "Tutorial";
    [SerializeField] private string difficultySceneName    = "DifficultySelect";
    [SerializeField] private string menuSceneName          = "";    // legacy fallback only

    // ── Runtime state ──────────────────────────────────────────────────────
    private enum IntroPhase { None, LogoConvergence, LogoHold, TitleReveal, Menu }
    private IntroPhase phase;
    private bool sequenceStarted;

    private Vector2 logoMainBasePos, logoCyanBasePos, logoMagentaBasePos;
    private Vector2 titlePlateBasePos, titleArtworkBasePos, subtitleBasePos;
    private Vector3 logoStackBaseScale, titlePlateBaseScale, titleArtworkBaseScale, subtitleBaseScale;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        CacheBaseTransforms();
        ResetVisualState();
        SetButtonsInteractable(false);
    }

    private void Start()
    {
        if (!sequenceStarted) { sequenceStarted = true; StartCoroutine(PlayIntroSequence()); }
    }

    private void Update()
    {
        HandleSkipInput();
        if (phase == IntroPhase.Menu && subtitleText != null && subtitleText.enabled)
            ApplySubtitleLiveEffects(1f);
    }

    private void OnEnable()  => SetButtonsInteractable(false);
    private void OnDisable() => StopAllCoroutines();
    private void OnDestroy() => StopAllCoroutines();

    // ── Cache / Reset ──────────────────────────────────────────────────────

    private void CacheBaseTransforms()
    {
        if (logoMain         != null) logoMainBasePos       = logoMain.rectTransform.anchoredPosition;
        if (logoGhostCyan    != null) logoCyanBasePos       = logoGhostCyan.rectTransform.anchoredPosition;
        if (logoGhostMagenta != null) logoMagentaBasePos    = logoGhostMagenta.rectTransform.anchoredPosition;
        if (logoStackRect    != null) logoStackBaseScale    = logoStackRect.localScale;
        if (titlePlateRect   != null) { titlePlateBasePos   = titlePlateRect.anchoredPosition;             titlePlateBaseScale   = titlePlateRect.localScale; }
        if (titleArtwork     != null) { titleArtworkBasePos = titleArtwork.rectTransform.anchoredPosition; titleArtworkBaseScale = titleArtwork.rectTransform.localScale; }
        if (subtitleText     != null) { subtitleBasePos     = subtitleText.rectTransform.anchoredPosition; subtitleBaseScale     = subtitleText.rectTransform.localScale; }
    }

    private void ResetVisualState()
    {
        if (introGroup     != null) introGroup.alpha = 1f;
        if (screenBackdrop != null) { screenBackdrop.enabled = true; screenBackdrop.color = Color.black; }

        SetAlpha(logoMain,          0f);
        SetAlpha(logoGhostCyan,     0f);
        SetAlpha(logoGhostMagenta,  0f);
        SetAlpha(titlePlate,        0f);
        SetAlpha(titleArtwork,      0f);
        SetAlpha(menuBackdropImage, 0f);
        SetAlpha(scanlineOverlay,   0f);

        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        if (menuGroup    != null) menuGroup.alpha = 0f;
        if (menuBackdropImage != null && menuBackgroundSprite != null)
            menuBackdropImage.sprite = menuBackgroundSprite;
    }

    // ══════════════════════════════════════════════════════════════════════
    // MASTER SEQUENCE
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayIntroSequence()
    {
        if (musicSource != null && introMusic != null)
        {
            musicSource.clip  = introMusic;
            musicSource.loop  = false;
            musicSource.time  = 0f;
            musicSource.Play();
        }

        yield return PlayTerminalBoot();
        yield return PlayLogoConvergence();
        yield return HoldLogoUntilBeatDrop();
        yield return FadeLogoOut();
        yield return PlayTitleReveal();
        yield return PlayMenuReveal();
    }

    // ══════════════════════════════════════════════════════════════════════
    // PART 1-A — TERMINAL BOOT (~0.4 s)
    // Simulates a CRT or neon sign powering on cold:
    //   • hard bright white flash (cathode ray / gas ignition)
    //   • rapid cyan/grey flicker (warming up)
    //   • settles to black
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayTerminalBoot()
    {
        // Hard CRT power-on flash — 3 frames of overexposed white
        if (screenBackdrop != null) screenBackdrop.color = Color.white;
        yield return null; yield return null; yield return null;

        // Rapid grey/cyan flicker then quench to black
        float elapsed = 0f, dur = 0.38f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t       = elapsed / dur;
            float flicker = Mathf.Clamp01(Mathf.PerlinNoise(Time.time * 38f, 0f) * (1f - t) * 1.6f);
            // Tint goes from grey/white → cyan → black as it quenches
            float cyanT   = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2f - 0.2f));
            if (screenBackdrop != null)
                screenBackdrop.color = new Color(
                    flicker * Mathf.Lerp(1f, 0.05f, cyanT),
                    flicker * Mathf.Lerp(1f, 0.9f,  cyanT),
                    flicker,
                    1f - Mathf.SmoothStep(0f, 1f, t)
                );
            yield return null;
        }
        if (screenBackdrop != null) screenBackdrop.color = Color.black;
        yield return new WaitForSeconds(0.12f);   // breath before ghosts appear
    }

    // ══════════════════════════════════════════════════════════════════════
    // PART 1-B — CHROMATIC CONVERGENCE
    // Four distinct sub-phases in one timed loop:
    //
    //  [0 → emergenceDuration]
    //    EMERGE: Cyan and magenta ghosts flicker in at opposite offsets.
    //    Looks like a neon sign trying to start — organic, noisy.
    //
    //  [emergenceDuration → +convergenceDuration]
    //    CONVERGE: Ghosts drift toward each other. Offset follows t³ so
    //    they linger apart then rush together in the last 20 %.
    //    Perpendicular wobble builds tension, fades as they approach.
    //    Main logo pre-glows faintly as gap closes.
    //
    //  [convergeEnd → +0.35 s]
    //    FUSE: Both ghosts snap to base position. Logo overexposes to
    //    bright white (color values >> 1). Screen flashes white — the
    //    two colours merging unlocks the light.
    //
    //  [fuseEnd → +logoSettleDuration]
    //    SETTLE: Logo dims from 4× overexposed back to 1×. Ghosts gone.
    //    Gentle pulse breathing begins.
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayLogoConvergence()
    {
        phase = IntroPhase.LogoConvergence;
        float pulsePeriod = 60f / Mathf.Max(1f, pulseBpm);

        float emergeEnd  = emergenceDuration;
        float convergeEnd= emergeEnd + convergenceDuration;
        float fuseEnd    = convergeEnd + 0.35f;
        float settleEnd  = fuseEnd + logoSettleDuration;
        float elapsed    = 0f;

        while (elapsed < settleEnd)
        {
            elapsed += Time.deltaTime;

            if (elapsed < emergeEnd)
            {
                // ── EMERGE ─────────────────────────────────────────────────
                float t = elapsed / emergeEnd;

                // Independent Perlin noise per ghost — different starting phases
                float noiseC = Mathf.PerlinNoise(elapsed * 8.3f, 0.31f);
                float noiseM = Mathf.PerlinNoise(1.77f, elapsed * 9.1f);
                // Build envelope ramps up slowly (neon warming up)
                float build  = Mathf.SmoothStep(0f, 1f, t) * 0.62f;
                float cAlpha = Mathf.Clamp01(Mathf.Clamp01(noiseC * 1.5f - 0.12f) + build);
                float mAlpha = Mathf.Clamp01(Mathf.Clamp01(noiseM * 1.5f - 0.18f) + build * 0.92f);

                // Position: fixed at starting offsets (not moving yet)
                if (logoGhostCyan    != null) { logoGhostCyan.rectTransform.anchoredPosition    = logoCyanBasePos    + new Vector2(-convergenceStartOffset, 0f); logoGhostCyan.color    = new Color(0f, 1f, 1f,    cAlpha); }
                if (logoGhostMagenta != null) { logoGhostMagenta.rectTransform.anchoredPosition = logoMagentaBasePos + new Vector2( convergenceStartOffset, 0f); logoGhostMagenta.color = new Color(1f, 0.3f, 0.9f, mAlpha); }
                SetAlpha(logoMain, 0f);
            }
            else if (elapsed < convergeEnd)
            {
                // ── CONVERGE ───────────────────────────────────────────────
                float t = (elapsed - emergeEnd) / convergenceDuration;

                // Offset stays high until the last ~25 % then rushes to 0 (cubic ease-in)
                float offset = convergenceStartOffset * (1f - t * t * t);

                // Perpendicular wobble: reduces to 0 as they close in; frequency rises
                float wobbleAmp  = 15f * Mathf.Clamp01(1f - t * 1.2f);
                float wobbleFreq = Mathf.Lerp(1.8f, 6f, t);
                float wobble     = Mathf.Sin(elapsed * wobbleFreq * Mathf.PI * 2f) * wobbleAmp;

                // Brightness and chroma separation both ramp up as gap closes
                float closeness  = t * t;
                float brightness = Mathf.Lerp(0.85f, 2f, closeness);
                float chroma     = Mathf.Lerp(chromaticOffsetPixels * 0.5f, 0f, t);

                if (logoGhostCyan != null)
                {
                    logoGhostCyan.rectTransform.anchoredPosition = logoCyanBasePos + new Vector2(-offset - chroma, wobble);
                    logoGhostCyan.color = new Color(0f, 1f, 1f, Mathf.Min(1f, brightness));
                }
                if (logoGhostMagenta != null)
                {
                    logoGhostMagenta.rectTransform.anchoredPosition = logoMagentaBasePos + new Vector2(offset + chroma, -wobble);
                    logoGhostMagenta.color = new Color(1f, 0.3f, 0.9f, Mathf.Min(1f, brightness));
                }

                // Pre-fusion glow: main logo materializes faintly in the last 30 %
                float preGlow = Mathf.Clamp01((t - 0.7f) / 0.3f);
                if (logoMain != null) logoMain.color = new Color(2f, 2f, 2f, preGlow * 0.3f);
            }
            else if (elapsed < fuseEnd)
            {
                // ── FUSE ───────────────────────────────────────────────────
                float t = (elapsed - convergeEnd) / (fuseEnd - convergeEnd);

                // Ghosts converge to base and fade
                if (logoGhostCyan    != null) { logoGhostCyan.rectTransform.anchoredPosition    = logoCyanBasePos;    logoGhostCyan.color    = new Color(0f, 1f, 1f,    1f - t); }
                if (logoGhostMagenta != null) { logoGhostMagenta.rectTransform.anchoredPosition = logoMagentaBasePos; logoGhostMagenta.color = new Color(1f, 0.3f, 0.9f, 1f - t); }

                // Logo: spikes to 4× overexposed white at t=0.25, holds high
                float glow = t < 0.25f
                    ? Mathf.Lerp(0.3f, 4f,  t / 0.25f)
                    : Mathf.Lerp(4f,   3.2f, (t - 0.25f) / 0.75f);
                if (logoMain != null) logoMain.color = new Color(glow, glow, glow, 1f);

                // Screen: pure white bell-curve flash
                float flashA = Mathf.Sin(Mathf.Clamp01(t * 2.2f) * Mathf.PI) * 0.82f;
                if (screenBackdrop != null) screenBackdrop.color = new Color(1f, 1f, 1f, flashA);
            }
            else
            {
                // ── SETTLE ─────────────────────────────────────────────────
                float t      = Mathf.Clamp01((elapsed - fuseEnd) / logoSettleDuration);
                float settle = Mathf.SmoothStep(0f, 1f, t);

                // Ghosts gone
                SetAlpha(logoGhostCyan,    0f);
                SetAlpha(logoGhostMagenta, 0f);

                // Logo dims from 3.2× bright → 1×
                float glow = Mathf.Lerp(3.2f, 1f, settle);
                if (logoMain != null) logoMain.color = new Color(glow, glow, glow, 1f);

                // Backdrop clears; breathing starts once settled
                if (screenBackdrop != null) screenBackdrop.color = Color.black;
                float breathe = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f / pulsePeriod) * pulseGlowAmount * settle;
                if (logoStackRect != null) logoStackRect.localScale = logoStackBaseScale * breathe;
            }

            yield return null;
        }

        // Lock final state
        if (logoMain     != null) logoMain.color = Color.white;
        if (logoStackRect != null) logoStackRect.localScale = logoStackBaseScale;
        SetAlpha(logoGhostCyan,    0f);
        SetAlpha(logoGhostMagenta, 0f);
        if (screenBackdrop != null) screenBackdrop.color = Color.black;
        phase = IntroPhase.LogoHold;
    }

    // Logo breathes on black until the music hits beatDropTime.
    // Minimum hold so it doesn't vanish instantly if beat is already close.
    private IEnumerator HoldLogoUntilBeatDrop()
    {
        float period = 60f / Mathf.Max(1f, pulseBpm);
        float elapsed = 0f, minHold = 1.2f;
        bool waitingForMusic = musicSource != null && musicSource.isPlaying;

        while (elapsed < minHold || (waitingForMusic && musicSource.time < beatDropTime))
        {
            elapsed += Time.deltaTime;
            if (!musicSource.isPlaying) waitingForMusic = false;
            float breathe = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f / period) * pulseGlowAmount;
            if (logoMain     != null) logoMain.color     = Color.white;
            if (logoStackRect != null) logoStackRect.localScale = logoStackBaseScale * breathe;
            yield return null;
        }
    }

    private IEnumerator FadeLogoOut()
    {
        Color mainC = logoMain != null ? logoMain.color : Color.clear;
        float mA = mainC.a, elapsed = 0f;

        while (elapsed < logoFadeOutLength)
        {
            elapsed += Time.deltaTime;
            float fade = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(elapsed / logoFadeOutLength));
            if (logoMain != null) { mainC.a = mA * fade; logoMain.color = mainC; }
            yield return null;
        }
        SetAlpha(logoMain, 0f);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PART 2 — TITLE REVEAL  ("System Reinitialization")
    // Techy, mechanical, slightly unstable — like old hardware rebooting.
    // Five beats in strict order, each with its own character:
    //
    //   ① Terminal scan reveal    — CRT raster sweeps the background on
    //   ② Plate horizontal wipe   — mechanical beam expands from center
    //   ③ Artwork corrupt→resolve — data stream locks onto the signal
    //   ④ Subtitle glitch-in      — text resolves from noise
    //   ⑤ Hold with micro-glitches — system is "live" but slightly unstable
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayTitleReveal()
    {
        phase = IntroPhase.TitleReveal;
        float pulsePeriod = 60f / Mathf.Max(1f, pulseBpm);

        yield return TerminalScanReveal();
        yield return new WaitForSeconds(0.06f);
        yield return TitlePlateMechanicalWipe();
        yield return new WaitForSeconds(0.08f);
        yield return TitleArtworkCorruptResolve();
        yield return new WaitForSeconds(0.06f);
        yield return PlaySubtitleReveal();
        yield return TitleHoldWithGlitches(pulsePeriod);
    }

    // ① Black screen wipes away via a CRT raster scan, revealing the
    //   background at reduced alpha. Scanline overlay (if assigned) flickers
    //   on and out. screenBackdrop goes from solid → cyan-tinted → transparent.
    private IEnumerator TerminalScanReveal()
    {
        if (menuBackdropImage != null) menuBackdropImage.enabled = true;

        if (scanlineOverlay != null)
        {
            scanlineOverlay.gameObject.SetActive(true);
            SetAlpha(scanlineOverlay, 0.75f);
        }

        float elapsed = 0f;
        while (elapsed < bgRevealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bgRevealDuration);

            // Background fades in at reduced alpha (dark & atmospheric)
            if (menuBackdropImage != null)
                menuBackdropImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, bgAlpha, t));

            // screenBackdrop: high-frequency CRT flicker (60 Hz alternation) fading out
            float scanFlicker = Mathf.Floor(Time.time * 58f) % 2f == 0f ? 1f : 0.78f;
            float alpha       = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, t)) * scanFlicker;
            // Colour transitions from solid black → cyan tint → transparent
            float cyanLift    = Mathf.SmoothStep(0.15f, 0.7f, t);
            if (screenBackdrop != null)
                screenBackdrop.color = new Color(
                    (1f - cyanLift * 0.75f) * scanFlicker,
                    scanFlicker,
                    scanFlicker,
                    alpha
                );

            // Scanlines fade out in the back half
            if (scanlineOverlay != null)
                SetAlpha(scanlineOverlay, Mathf.Lerp(0.75f, 0f, Mathf.SmoothStep(0.4f, 1f, t)));

            yield return null;
        }

        if (menuBackdropImage != null) menuBackdropImage.color = new Color(1f, 1f, 1f, bgAlpha);
        if (screenBackdrop    != null) screenBackdrop.color    = Color.clear;
        if (scanlineOverlay   != null) scanlineOverlay.gameObject.SetActive(false);
    }

    // ② Title plate expands from zero width — a mechanical horizontal wipe
    //   as if a panel is deploying. EaseOutQuart feels like a pneumatic snap.
    //   Landing: brief edge flare then 2-4 frames of settling jitter.
    private IEnumerator TitlePlateMechanicalWipe()
    {
        if (titlePlate == null && titlePlateRect == null) yield break;

        // Start: full alpha, zero X scale
        SetAlpha(titlePlate, 1f);
        if (titlePlateRect != null) { titlePlateRect.anchoredPosition = titlePlateBasePos; titlePlateRect.localScale = new Vector3(0f, titlePlateBaseScale.y, titlePlateBaseScale.z); }

        float elapsed = 0f;
        while (elapsed < plateWipeDuration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / plateWipeDuration);
            float ease = EaseOutQuart(t);

            if (titlePlateRect != null)
                titlePlateRect.localScale = new Vector3(
                    Mathf.Lerp(0f, titlePlateBaseScale.x, ease),
                    titlePlateBaseScale.y,
                    titlePlateBaseScale.z
                );

            // Leading-edge cyan flare: brightest at low t, gone by mid-wipe
            float edgeGlow = Mathf.Clamp01((1f - t) * 1.6f - 0.3f);
            if (titlePlate != null)
                titlePlate.color = new Color(1f, 1f + edgeGlow * 0.15f, 1f + edgeGlow * 0.8f, 1f);

            yield return null;
        }

        if (titlePlateRect != null) titlePlateRect.localScale = titlePlateBaseScale;
        if (titlePlate     != null) titlePlate.color          = Color.white;

        // Post-wipe settling jitter: 2-4 frames of tiny X displacement
        int jitterFrames = Random.Range(2, 5);
        for (int i = 0; i < jitterFrames; i++)
        {
            if (titlePlateRect != null)
                titlePlateRect.anchoredPosition = titlePlateBasePos + new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(-1.2f, 1.2f));
            yield return null;
        }
        if (titlePlateRect != null) titlePlateRect.anchoredPosition = titlePlateBasePos;
    }

    // ③ Artwork appears at full alpha immediately but with heavy Perlin noise
    //   on its position. The noise decays with EaseInCubic so it starts slow
    //   then rapidly clears — signal lock-on. First half also strobes the alpha
    //   between 1.0 and 0.3 at ~20 Hz to sell the corrupted-data feel.
    private IEnumerator TitleArtworkCorruptResolve()
    {
        if (titleArtwork == null) yield break;

        titleArtwork.rectTransform.anchoredPosition = titleArtworkBasePos;
        titleArtwork.rectTransform.localScale       = titleArtworkBaseScale;
        SetAlpha(titleArtwork, 1f);

        float elapsed      = 0f;
        float strobeTimer  = 0f;
        float strobeHz     = 20f;
        bool  strobeHigh   = true;

        while (elapsed < artworkResolveDuration)
        {
            elapsed += Time.deltaTime;
            float t          = Mathf.Clamp01(elapsed / artworkResolveDuration);
            float corruption = 1f - EaseInCubic(t);   // decays slowly then rushes to 0

            // Noise position (signal jitter)
            Vector2 noiseOffset = new Vector2(
                (Mathf.PerlinNoise(Time.time * 50f, 0f)   - 0.5f) * artworkCorruptPixels * corruption,
                (Mathf.PerlinNoise(0f, Time.time * 46f)   - 0.5f) * artworkCorruptPixels * corruption * 0.4f
            );
            titleArtwork.rectTransform.anchoredPosition = titleArtworkBasePos + noiseOffset;

            // Alpha strobe in first 50 %, settles to 1 after
            if (t < 0.5f)
            {
                float interval = 1f / Mathf.Lerp(strobeHz, strobeHz * 0.5f, t * 2f);
                strobeTimer += Time.deltaTime;
                if (strobeTimer >= interval) { strobeTimer = 0f; strobeHigh = !strobeHigh; }
                SetAlpha(titleArtwork, strobeHigh ? 1f : 0.28f);
            }
            else
            {
                SetAlpha(titleArtwork, 1f);
            }

            yield return null;
        }

        titleArtwork.rectTransform.anchoredPosition = titleArtworkBasePos;
        SetAlpha(titleArtwork, 1f);
    }

    // ④ Subtitle glitches in — aggressive jitter that resolves to position.
    private IEnumerator PlaySubtitleReveal()
    {
        if (subtitleText == null) yield break;

        subtitleText.gameObject.SetActive(true);
        subtitleText.enabled = true;
        subtitleText.alpha   = 0f;
        subtitleText.rectTransform.anchoredPosition = subtitleBasePos + new Vector2(-subtitleGlitchJitterPixels * 2f, 0f);
        subtitleText.rectTransform.localScale       = subtitleBaseScale * 1.06f;

        float elapsed = 0f, glitchTimer = 0f, nextGlitch = Random.Range(0.03f, 0.18f);

        while (elapsed < subtitleEntranceLength)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextGlitch) { glitchTimer = Random.Range(0.03f, 0.11f); nextGlitch = elapsed + Random.Range(0.04f, 0.22f); }
            if (glitchTimer > 0f) glitchTimer -= Time.deltaTime;

            float t      = Mathf.Clamp01(elapsed / subtitleEntranceLength);
            float reveal = Mathf.SmoothStep(0f, 1f, t);
            float flicker= Mathf.Clamp01(Mathf.PerlinNoise(Time.time * 28f, 2.4f) * 1.3f - 0.2f);
            float burst  = glitchTimer > 0f ? Mathf.Clamp01(glitchTimer / 0.11f) : 0f;
            float alpha  = Mathf.Clamp01(reveal * 0.6f + burst * 0.6f + flicker * 0.22f);

            Vector2 jitter = new Vector2(
                (Mathf.PerlinNoise(Time.time * 36f, 8.7f) - 0.5f) * subtitleGlitchJitterPixels * (0.3f + burst * 1.8f + reveal * 0.7f),
                (Mathf.PerlinNoise(4.1f, Time.time * 30f) - 0.5f) * subtitleGlitchJitterPixels * 0.45f * (0.3f + burst * 1.8f)
            );

            subtitleText.rectTransform.anchoredPosition = subtitleBasePos + jitter;
            subtitleText.rectTransform.localScale       = subtitleBaseScale * (1f + reveal * 0.02f + burst * 0.025f);

            Color cyan    = new Color(0.3f, 0.95f, 1f,   alpha);
            Color magenta = new Color(1f,   0.4f,  0.95f, alpha);
            subtitleText.color = Color.Lerp(Color.Lerp(cyan, magenta, Mathf.Repeat(Time.time * 8f, 1f)), Color.white, reveal * 0.55f);
            subtitleText.alpha = alpha;

            yield return null;
        }

        subtitleText.alpha = 1f;
        subtitleText.rectTransform.anchoredPosition = subtitleBasePos;
        subtitleText.rectTransform.localScale       = subtitleBaseScale;
        ApplySubtitleLiveEffects(1f);
    }

    // ⑤ All elements breathe from one shared signal (no more independent alpha
    //   pulsing). Every 0.3-0.65 s a random element gets 1-3 frames of tiny
    //   jitter — the system is live, but slightly unstable.
    private IEnumerator TitleHoldWithGlitches(float pulsePeriod)
    {
        float elapsed    = 0f;
        float nextGlitch = Random.Range(0.3f, 0.65f);

        while (elapsed < titleHoldLength)
        {
            elapsed += Time.deltaTime;
            float lifeT   = Mathf.Clamp01(elapsed / titleHoldLength);
            float breathe = 1f + Mathf.Sin(Time.time * Mathf.PI * 2f / pulsePeriod) * titlePulseAmount * 0.65f;
            float micro   = Mathf.PerlinNoise(Time.time * 2.8f, 0.5f) * titlePulseAmount * 0.25f;

            // Scale breathes — alpha stays at 1 (fixes the pulsing/fading issue)
            if (titleArtwork   != null) titleArtwork.rectTransform.localScale = titleArtworkBaseScale * (breathe + micro);
            if (titlePlateRect != null) titlePlateRect.localScale             = titlePlateBaseScale   * (1f + (breathe - 1f) * 0.35f);
            if (subtitleText   != null) ApplySubtitleLiveEffects(lifeT);

            // Periodic micro-glitch on a random element
            if (elapsed >= nextGlitch)
            {
                nextGlitch = elapsed + Random.Range(0.28f, 0.68f);
                int which = Random.Range(0, 3);
                if      (which == 0 && titleArtwork  != null) StartCoroutine(MicroGlitch(titleArtwork.rectTransform, titleArtworkBasePos, 3.5f));
                else if (which == 1 && titlePlateRect != null) StartCoroutine(MicroGlitch(titlePlateRect,             titlePlateBasePos,   2.5f));
                else if (which == 2 && subtitleText   != null) StartCoroutine(MicroGlitch(subtitleText.rectTransform, subtitleBasePos,     3f));
            }

            yield return null;
        }

        // Reset all positions before menu reveal
        if (titleArtwork   != null) titleArtwork.rectTransform.anchoredPosition  = titleArtworkBasePos;
        if (titlePlateRect != null) titlePlateRect.anchoredPosition               = titlePlateBasePos;
        if (subtitleText   != null) subtitleText.rectTransform.anchoredPosition   = subtitleBasePos;
    }

    // Random element gets 1-3 frames of tiny displacement then snaps clean.
    private IEnumerator MicroGlitch(RectTransform target, Vector2 basePos, float intensity)
    {
        int frames = Random.Range(1, 4);
        for (int i = 0; i < frames; i++)
        {
            target.anchoredPosition = basePos + new Vector2(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity * 0.35f, intensity * 0.35f)
            );
            yield return null;
        }
        target.anchoredPosition = basePos;
    }

    // ══════════════════════════════════════════════════════════════════════
    // PART 3 — MENU REVEAL
    // Twin neon surge (cyan then magenta) vibrates the title elements just
    // before the menu buttons fade in — the whole UI powering to life.
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayMenuReveal()
    {
        yield return MenuSurgeGlitch();
        if (menuGroup != null)
            yield return FadeCanvasGroup(menuGroup, 0f, 1f, menuRevealLength);
        phase = IntroPhase.Menu;
        SetButtonsInteractable(true);
    }

    private IEnumerator MenuSurgeGlitch()
    {
        float elapsed = 0f, dur = 0.52f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;

            float flash1 = Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI) * 2.8f - 1.2f);
            float flash2 = Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01((t - 0.26f) / 0.4f) * Mathf.PI) * 2.8f - 1.2f);
            float total  = flash1 * 0.32f + flash2 * 0.28f;

            Color surgeColor = Color.Lerp(
                new Color(0f, 0.9f, 1f,   flash1 * 0.32f),
                new Color(1f, 0.2f, 0.9f, flash2 * 0.28f),
                flash2 / Mathf.Max(0.001f, flash1 + flash2)
            );
            surgeColor.a = total;
            if (screenBackdrop != null) screenBackdrop.color = surgeColor;

            if (total > 0.04f)
            {
                Vector2 shake = new Vector2(
                    (Mathf.PerlinNoise(Time.time * 62f, 0f) - 0.5f) * total * 4.5f,
                    (Mathf.PerlinNoise(0f, Time.time * 58f) - 0.5f) * total * 2f
                );
                if (titleArtwork   != null) titleArtwork.rectTransform.anchoredPosition  = titleArtworkBasePos + shake;
                if (titlePlateRect != null) titlePlateRect.anchoredPosition               = titlePlateBasePos  + shake * 0.55f;
                if (subtitleText   != null) subtitleText.rectTransform.anchoredPosition   = subtitleBasePos    + shake * 0.32f;
            }
            else
            {
                if (titleArtwork   != null) titleArtwork.rectTransform.anchoredPosition  = titleArtworkBasePos;
                if (titlePlateRect != null) titlePlateRect.anchoredPosition               = titlePlateBasePos;
                if (subtitleText   != null) subtitleText.rectTransform.anchoredPosition   = subtitleBasePos;
            }

            yield return null;
        }

        if (titleArtwork   != null) titleArtwork.rectTransform.anchoredPosition  = titleArtworkBasePos;
        if (titlePlateRect != null) titlePlateRect.anchoredPosition               = titlePlateBasePos;
        if (subtitleText   != null) subtitleText.rectTransform.anchoredPosition   = subtitleBasePos;
        if (screenBackdrop != null) screenBackdrop.color                          = Color.clear;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LIVE SUBTITLE EFFECTS  (Update fires this every frame in Menu phase)
    // ══════════════════════════════════════════════════════════════════════

    private void ApplySubtitleLiveEffects(float lifeT)
    {
        if (subtitleText == null || !subtitleText.enabled) return;

        float period     = 60f / Mathf.Max(1f, pulseBpm);
        float pulseWave  = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / period);
        float pulse      = pulseWave * pulseWave * subtitlePulseAmount * Mathf.Lerp(0.35f, 1f, lifeT);
        float glitchNoise= Mathf.PerlinNoise(Time.time * 7.2f, 1.7f);
        float threshold  = 1f - Mathf.Lerp(subtitleGlitchChance * 0.7f, subtitleGlitchChance, lifeT);

        if (glitchNoise > threshold)
        {
            Vector2 shift = new Vector2(
                (Mathf.PerlinNoise(Time.time * 51f, 3.3f) - 0.5f) * subtitleChromaticShiftPixels,
                (Mathf.PerlinNoise(5.7f, Time.time * 47f) - 0.5f) * subtitleChromaticShiftPixels * 0.35f
            );
            subtitleText.rectTransform.anchoredPosition = subtitleBasePos + shift * 0.3f;
            subtitleText.color = Mathf.Repeat(Time.time * 9f, 1f) < 0.5f
                ? new Color(0.35f, 1f, 1f,    subtitleText.alpha)
                : new Color(1f, 0.45f, 0.95f, subtitleText.alpha);
        }
        else
        {
            subtitleText.rectTransform.anchoredPosition = subtitleBasePos;
            float glow = 1f + pulse + Mathf.PerlinNoise(Time.time * 4.1f, 9.7f) * subtitlePulseAmount * 0.5f;
            Color c = subtitleText.color;
            c.r = Mathf.Clamp01(c.r * glow); c.g = Mathf.Clamp01(c.g * glow); c.b = Mathf.Clamp01(c.b * glow);
            subtitleText.color = c;
        }

        subtitleText.rectTransform.localScale = subtitleBaseScale *
            (1f + pulse + Mathf.PerlinNoise(Time.time * 3.5f, 6.4f) * subtitlePulseAmount * 0.35f);
    }

    // ── Skip ───────────────────────────────────────────────────────────────

    private void HandleSkipInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            SkipToMenu();
    }

    private void SkipToMenu()
    {
        if (!sequenceStarted) return;
        StopAllCoroutines();
        sequenceStarted = false;

        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
        if (screenBackdrop != null) screenBackdrop.color = Color.clear;
        if (menuGroup      != null) menuGroup.alpha      = 1f;

        SetAlpha(logoMain,         0f);
        SetAlpha(logoGhostCyan,    0f);
        SetAlpha(logoGhostMagenta, 0f);

        if (menuBackdropImage != null) { menuBackdropImage.enabled = true; menuBackdropImage.color = new Color(1f, 1f, 1f, bgAlpha); }
        SetAlpha(titlePlate,   1f);
        SetAlpha(titleArtwork, 1f);
        if (titlePlateRect   != null) { titlePlateRect.anchoredPosition   = titlePlateBasePos;   titlePlateRect.localScale   = titlePlateBaseScale; }
        if (titleArtwork     != null) { titleArtwork.rectTransform.anchoredPosition = titleArtworkBasePos; titleArtwork.rectTransform.localScale = titleArtworkBaseScale; }
        if (subtitleText     != null) { subtitleText.gameObject.SetActive(true); subtitleText.alpha = 1f; subtitleText.rectTransform.anchoredPosition = subtitleBasePos; }

        phase = IntroPhase.Menu;
        SetButtonsInteractable(true);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private IEnumerator FadeGraphic(Graphic g, float from, float to, float dur)
    {
        if (g == null) yield break;
        Color c = g.color; float elapsed = 0f;
        while (elapsed < dur) { elapsed += Time.deltaTime; c.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / dur)); g.color = c; yield return null; }
        c.a = to; g.color = c;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur) { elapsed += Time.deltaTime; cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / dur)); yield return null; }
        cg.alpha = to;
    }

    // t³ — slow start, fast finish. Used for corruption decay (resolve rushes at end).
    private static float EaseInCubic(float t)  => t * t * t;

    // 1 − (1−t)⁴ — fast start, settles. Used for the mechanical wipe.
    private static float EaseOutQuart(float t)  { float s = 1f - t; return 1f - s * s * s * s; }

    // Classic spring overshoot.
    private static float EaseOutBack(float t, float overshoot = 1.70158f) { t -= 1f; return t * t * ((overshoot + 1f) * t + overshoot) + 1f; }

    private void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color; c.a = a; g.color = c;
    }

    private void SetButtonsInteractable(bool on)
    {
        if (menuButtons != null) foreach (Button b in menuButtons)               if (b != null) b.interactable = on;
        if (neonButtons != null) foreach (NeonUIButtonAnimator n in neonButtons)  if (n != null) n.SetActive(on);
    }

    // ── Public button methods ───────────────────────────────────────────────

    public void StartGame()       => OnPlayPressed();   // legacy compat for existing button wiring

    public void OnPlayPressed()   => StartCoroutine(PlayTransitionCoroutine());
    public void OnOptionsPressed()=> StartCoroutine(OptionsTransitionCoroutine());
    public void OnExitPressed()   => StartCoroutine(ExitTransitionCoroutine());

    // ── PLAY — Focus zoom, fade to black, load tutorial or game ────────────

    private IEnumerator PlayTransitionCoroutine()
    {
        SetButtonsInteractable(false);

        // 1. Everything dims away — title artwork stays isolated (0.5 s)
        float elapsed = 0f;
        while (elapsed < 0.52f)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.52f));
            if (menuGroup        != null) menuGroup.alpha = fade;
            if (menuBackdropImage!= null) { Color c = menuBackdropImage.color; c.a = bgAlpha * fade; menuBackdropImage.color = c; }
            SetAlpha(titlePlate,    fade);
            if (subtitleText     != null) subtitleText.alpha = fade;
            yield return null;
        }
        if (menuGroup != null) menuGroup.alpha = 0f;
        SetAlpha(titlePlate, 0f);
        if (subtitleText != null) subtitleText.alpha = 0f;

        // 2. Title artwork zooms — grows while chromatic ghosts bleed in (0.88 s)
        elapsed = 0f;
        Vector3 zoomStart = titleArtwork != null ? titleArtwork.rectTransform.localScale : Vector3.one;
        Vector3 zoomEnd   = zoomStart * 2.6f;

        while (elapsed < 0.88f)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / 0.88f);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            if (titleArtwork != null)
                titleArtwork.rectTransform.localScale = Vector3.Lerp(zoomStart, zoomEnd, ease * 0.55f);

            // Chromatic aberration bleeds in as the zoom builds
            float cT = t * t;
            float cOff = chromaticOffsetPixels * cT * 2.2f;
            if (logoGhostCyan    != null) { logoGhostCyan.rectTransform.anchoredPosition    = logoCyanBasePos    + new Vector2(-cOff, 0f); logoGhostCyan.color    = new Color(0f, 1f, 1f,    cT * 0.55f); }
            if (logoGhostMagenta != null) { logoGhostMagenta.rectTransform.anchoredPosition = logoMagentaBasePos + new Vector2( cOff, 0f); logoGhostMagenta.color = new Color(1f, 0.3f, 0.9f, cT * 0.55f); }

            yield return null;
        }

        // 3. Fade to black while zoom continues (0.5 s); music fades out in parallel
        elapsed = 0f;
        Vector3 midScale = titleArtwork != null ? titleArtwork.rectTransform.localScale : zoomStart;
        float musicStartVol = musicSource != null ? musicSource.volume : 1f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.5f);

            if (titleArtwork != null)
                titleArtwork.rectTransform.localScale = Vector3.Lerp(midScale, zoomEnd, t);

            if (screenBackdrop != null) screenBackdrop.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t));

            float cFade = 1f - t;
            SetAlpha(logoGhostCyan,    cFade * 0.55f);
            SetAlpha(logoGhostMagenta, cFade * 0.55f);

            if (musicSource != null) musicSource.volume = Mathf.Lerp(musicStartVol, 0f, t);

            yield return null;
        }

        if (screenBackdrop != null) screenBackdrop.color = Color.black;
        SetAlpha(logoGhostCyan,    0f);
        SetAlpha(logoGhostMagenta, 0f);
        if (musicSource != null) { musicSource.volume = 0f; musicSource.Stop(); }

        yield return new WaitForSeconds(0.12f);

        bool tutorialSeen = GameProgress.Instance != null && GameProgress.Instance.HasSeenTutorial;
        string target = (!tutorialSeen && !string.IsNullOrWhiteSpace(tutorialSceneName))
            ? tutorialSceneName
            : (!string.IsNullOrWhiteSpace(difficultySceneName) ? difficultySceneName : menuSceneName);

        if (!string.IsNullOrWhiteSpace(target))
            SceneManager.LoadScene(target);
    }

    // ── OPTIONS — placeholder until Options screen is built ───────────────────

    private IEnumerator OptionsTransitionCoroutine()
    {
        // Not yet implemented — restore buttons immediately
        yield return null;
        SetButtonsInteractable(true);
    }

    // ── EXIT — CRT TV turn-off, then Application.Quit ──────────────────────

    private IEnumerator ExitTransitionCoroutine()
    {
        SetButtonsInteractable(false);

        // Quick dark-out of all other UI (0.18 s)
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.18f);
            if (menuGroup         != null) menuGroup.alpha = 1f - t;
            if (menuBackdropImage != null) { Color c = menuBackdropImage.color; c.a = bgAlpha * (1f - t); menuBackdropImage.color = c; }
            SetAlpha(titlePlate,  1f - t);
            SetAlpha(titleArtwork, 1f - t);
            if (subtitleText != null) subtitleText.alpha = 1f - t;
            yield return null;
        }

        // Neon sign losing power — cyan/magenta oscillation, amplitude decaying,
        // frequency rising (the sign gets faster right before it dies). (0.45 s)
        elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.deltaTime;
            float t         = Mathf.Clamp01(elapsed / 0.45f);
            float amplitude = 1f - EaseInCubic(t);                    // holds up then rushes to 0
            float freq      = Mathf.Lerp(3f, 22f, t);                 // frequency climbs as power drops
            float wave      = Mathf.Sin(elapsed * freq * Mathf.PI * 2f) * 0.5f + 0.5f;
            float flicker   = Mathf.PerlinNoise(Time.time * 26f, 0.7f) * 0.55f + 0.45f;
            if (screenBackdrop != null)
                screenBackdrop.color = Color.Lerp(
                    new Color(0f,  0.85f, 1f,    amplitude * flicker),   // cyan
                    new Color(1f,  0.2f,  0.85f, amplitude * flicker),   // magenta
                    wave
                );
            yield return null;
        }

        // CRT capacitor discharge — final surge to white (0.1 s)
        if (screenBackdrop != null)
        {
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                screenBackdrop.color = new Color(1f, 1f, 1f, Mathf.Clamp01(elapsed / 0.1f));
                yield return null;
            }
            screenBackdrop.color = Color.white;
        }

        // Squish Y → thin line (0.32 s, EaseInCubic — slow start, rushes in)
        RectTransform bdRect = screenBackdrop != null ? screenBackdrop.rectTransform : null;
        Vector3 origScale    = bdRect != null ? bdRect.localScale : Vector3.one;

        elapsed = 0f;
        while (elapsed < 0.32f)
        {
            elapsed += Time.deltaTime;
            float t = EaseInCubic(Mathf.Clamp01(elapsed / 0.32f));
            if (bdRect != null)
                bdRect.localScale = new Vector3(origScale.x, Mathf.Lerp(origScale.y, origScale.y * 0.025f, t), origScale.z);
            yield return null;
        }

        yield return new WaitForSeconds(0.07f);   // hold the white line

        // Contract X → gone (0.14 s)
        elapsed = 0f;
        Vector3 lineScale = bdRect != null ? bdRect.localScale : origScale;
        while (elapsed < 0.14f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.14f);
            if (bdRect != null)
                bdRect.localScale = new Vector3(Mathf.Lerp(lineScale.x, 0f, t), lineScale.y, origScale.z);
            yield return null;
        }

        // Reset, solid black, wait, quit
        if (bdRect != null) bdRect.localScale = origScale;
        if (screenBackdrop != null) screenBackdrop.color = Color.black;

        yield return new WaitForSeconds(0.25f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
