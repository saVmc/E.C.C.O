using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class CinematicIntroController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private float beatDropTime = 23f;

    [Header("Intro Layers")]
    [FormerlySerializedAs("rootGroup")]
    [SerializeField] private CanvasGroup introGroup;
    [FormerlySerializedAs("blackBackdrop")]
    [SerializeField] private Image screenBackdrop;
    [FormerlySerializedAs("logoSplash")]
    [SerializeField] private Image logoMain;
    [SerializeField] private Image logoGhostCyan;
    [SerializeField] private Image logoGhostMagenta;
    [SerializeField] private RectTransform logoStackRect;
    [FormerlySerializedAs("titleBackdrop")]
    [SerializeField] private Image titlePlate;
    [FormerlySerializedAs("titleBackdropRect")]
    [SerializeField] private RectTransform titlePlateRect;
    [FormerlySerializedAs("titleImage")]
    [SerializeField] private Image titleArtwork;
    [SerializeField] private Sprite menuBackgroundSprite;
    [FormerlySerializedAs("menuBackgroundImage")]
    [SerializeField] private Image menuBackdropImage;

    [Header("Menu")]
    [FormerlySerializedAs("menuGroup")]
    [SerializeField] private CanvasGroup menuGroup;
    [FormerlySerializedAs("menuButtons")]
    [SerializeField] private Button[] menuButtons;
    [FormerlySerializedAs("neonButtons")]
    [SerializeField] private NeonUIButtonAnimator[] neonButtons;
    [FormerlySerializedAs("subtitleText")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("Timing")]
    [SerializeField] private float logoFadeInLength = 2.4f;
    [SerializeField] private float logoFlickerLength = 8.5f;
    [SerializeField] private float logoStabilizeLength = 3.2f;
    [SerializeField] private float logoHoldLength = 2.2f;
    [SerializeField] private float logoFadeOutLength = 2.1f;
    [SerializeField] private float titleBuildLength = 1.2f;
    [SerializeField] private float titleHoldLength = 1.6f;
    [SerializeField] private float menuRevealLength = 1.0f;

    [Header("Motion")]
    [SerializeField] private float logoJitterPixels = 2.5f;
    [SerializeField] private float chromaticOffsetPixels = 9f;
    [SerializeField] private float titlePulseAmount = 0.05f;

    [Header("Title Text")]
    [SerializeField] private float subtitleEntranceLength = 1.15f;
    [SerializeField] private float subtitlePulseAmount = 0.012f;
    [SerializeField] private float subtitleGlitchChance = 0.18f;
    [SerializeField] private float subtitleGlitchJitterPixels = 4.5f;
    [SerializeField] private float subtitleChromaticShiftPixels = 5f;

    [Header("Neon Pulse")]
    [SerializeField] private float pulseBpm = 82f;
    [SerializeField] private float pulseGlowAmount = 0.032f;
    [SerializeField] private float pulseChromaticAmount = 0.25f;
    [SerializeField] private float pulseEmissionAmount = 0.08f;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "";

    private bool sequenceStarted;
    private Vector2 logoMainBasePosition;
    private Vector2 logoCyanBasePosition;
    private Vector2 logoMagentaBasePosition;
    private Vector2 titlePlateBasePosition;
    private Vector2 titleArtworkBasePosition;
    private Vector2 subtitleBasePosition;
    private Vector3 logoStackBaseScale = Vector3.one;
    private Vector3 titlePlateBaseScale = Vector3.one;
    private Vector3 titleArtworkBaseScale = Vector3.one;
    private Vector3 subtitleBaseScale = Vector3.one;
    private float logoIgnitionStartTime;

    private void Awake()
    {
        CacheInitialTransforms();
        ResetVisualState();
        SetButtonsInteractable(false);
    }

    private void Start()
    {
        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(PlayIntroSequence());
        }
    }

    private void CacheInitialTransforms()
    {
        if (logoMain != null)
            logoMainBasePosition = logoMain.rectTransform.anchoredPosition;

        if (logoGhostCyan != null)
            logoCyanBasePosition = logoGhostCyan.rectTransform.anchoredPosition;

        if (logoGhostMagenta != null)
            logoMagentaBasePosition = logoGhostMagenta.rectTransform.anchoredPosition;

        if (logoStackRect != null)
            logoStackBaseScale = logoStackRect.localScale;

        if (titlePlateRect != null)
        {
            titlePlateBasePosition = titlePlateRect.anchoredPosition;
            titlePlateBaseScale = titlePlateRect.localScale;
        }

        if (titleArtwork != null)
        {
            titleArtworkBasePosition = titleArtwork.rectTransform.anchoredPosition;
            titleArtworkBaseScale = titleArtwork.rectTransform.localScale;
        }

        if (subtitleText != null)
        {
            subtitleBasePosition = subtitleText.rectTransform.anchoredPosition;
            subtitleBaseScale = subtitleText.rectTransform.localScale;
        }
    }

    private void ResetVisualState()
    {
        if (introGroup != null)
            introGroup.alpha = 1f;

        if (screenBackdrop != null)
        {
            screenBackdrop.enabled = true;
            screenBackdrop.color = Color.black;
        }

        SetLogoGraphicState(logoMain, Color.black, 0f);
        SetLogoGraphicState(logoGhostCyan, Color.black, 0f);
        SetLogoGraphicState(logoGhostMagenta, Color.black, 0f);
        SetImageAlpha(titlePlate, 0f);
        SetImageAlpha(titleArtwork, 0f);
        SetImageAlpha(menuBackdropImage, 100f);

        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(false);
        }

        if (menuGroup != null)
            menuGroup.alpha = 0f;

        if (menuBackdropImage != null && menuBackgroundSprite != null)
            menuBackdropImage.sprite = menuBackgroundSprite;

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        SetButtonsInteractable(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            SkipToMenu();
    }

    private IEnumerator PlayIntroSequence()
    {
        if (musicSource != null && introMusic != null)
        {
            musicSource.clip = introMusic;
            musicSource.loop = false;
            musicSource.time = 0f;
            musicSource.Play();
        }

        yield return PlayLogoIgnition();
        yield return WaitForBeatDrop();
        yield return PlayTitleReveal();
        yield return PlayMenuReveal();
    }

    private IEnumerator PlayLogoIgnition()
    {
        logoIgnitionStartTime = Time.time;

        float elapsed = 0f;
        float sparkElapsed = 0f;
        float sparkDuration = 0f;
        float sparkStrength = 0f;
        float nextSparkAt = Random.Range(0.02f, 0.22f);
        float ignitionCharge = 0f;
        int sparkCount = 0;
        Vector2 logoBasePosition = logoMain != null ? logoMain.rectTransform.anchoredPosition : Vector2.zero;
        Vector3 logoBaseScale = logoStackRect != null ? logoStackRect.localScale : Vector3.one;
        float pulsePeriod = 60f / Mathf.Max(1f, pulseBpm);

        float totalLength = logoFadeInLength + logoFlickerLength + logoStabilizeLength + logoHoldLength;

        while (elapsed < totalLength)
        {
            elapsed += Time.deltaTime;

            float unstableT = Mathf.Clamp01((elapsed - logoFadeInLength) / logoFlickerLength);
            float stabilizeT = Mathf.Clamp01((elapsed - logoFadeInLength - logoFlickerLength) / logoStabilizeLength);
            float holdT = Mathf.Clamp01((elapsed - logoFadeInLength - logoFlickerLength - logoStabilizeLength) / logoHoldLength);
            float brightnessT = Mathf.Clamp01(Mathf.Max(ignitionCharge, stabilizeT * 0.85f + holdT));
            float flickerDamping = Mathf.Lerp(1f, 0.28f, brightnessT);

            if (elapsed <= logoFadeInLength + logoFlickerLength && elapsed >= nextSparkAt)
            {
                sparkElapsed = 0f;
                sparkDuration = (Random.Range(0.02f, 0.12f) + Mathf.Lerp(0.06f, 0.015f, unstableT)) * flickerDamping;
                sparkStrength = Random.Range(0.2f, 1f) * flickerDamping;
                sparkCount++;
                nextSparkAt = elapsed + (Random.Range(0.05f, 0.5f) + (1f - unstableT) * Random.Range(0.06f, 0.18f)) * Mathf.Lerp(1f, 1.9f, brightnessT);
            }

            if (sparkDuration > 0f)
            {
                sparkElapsed += Time.deltaTime;
                if (sparkElapsed >= sparkDuration)
                {
                    sparkDuration = 0f;
                    sparkStrength = 0f;
                }
            }

            float sparkT = sparkDuration > 0f ? 1f - Mathf.Clamp01(sparkElapsed / sparkDuration) : 0f;
            sparkT *= sparkT;

            float noiseA = Mathf.PerlinNoise(Time.time * 6.5f, 0.25f);
            float noiseB = Mathf.PerlinNoise(0.75f, Time.time * 7.5f);
            float unstableNoise = Mathf.Clamp01((noiseA + noiseB) * 0.5f);
            float chargeNoise = Mathf.PerlinNoise(Time.time * 1.2f, 9.1f);

            if (sparkT > 0.01f)
                ignitionCharge += Time.deltaTime * Mathf.Lerp(0.08f, 0.24f, sparkStrength) * Mathf.Lerp(1f, 0.8f, brightnessT);
            else if (elapsed < logoFadeInLength + logoFlickerLength)
                ignitionCharge += Time.deltaTime * Mathf.Lerp(0.02f, 0.08f, unstableT) * Mathf.Lerp(1f, 0.65f, brightnessT);
            else
                ignitionCharge += Time.deltaTime * Mathf.Lerp(0.06f, 0.12f, stabilizeT) * 0.9f;

            ignitionCharge = Mathf.Clamp01(ignitionCharge);

            float stepProgress = Mathf.Clamp01((Mathf.Floor(ignitionCharge * 6f) + Mathf.Clamp01(chargeNoise * 0.7f)) / 6f);
            float flickerEnvelope = Mathf.Clamp01(sparkT * sparkStrength * Mathf.Lerp(1.3f, 2.1f, unstableT) + unstableNoise * 0.18f);
            float buildEnvelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ignitionCharge - 0.22f) / 0.58f));
            float lockEnvelope = Mathf.SmoothStep(0f, 1f, stabilizeT);
            float holdEnvelope = Mathf.SmoothStep(0f, 1f, holdT);

            float pulsePhase = (Time.time - logoIgnitionStartTime) / pulsePeriod;
            float pulseWave = 0.5f + 0.5f * Mathf.Sin(pulsePhase * Mathf.PI * 2f);
            float pulse = (pulseWave * pulseWave) * holdEnvelope;
            float rareJolt = (sparkCount > 4 && stabilizeT > 0f) ? Mathf.Pow(Mathf.PerlinNoise(Time.time * 13.4f, 2.1f), 7f) : 0f;

            float glowBoost = 1f +
                flickerEnvelope * 1.15f +
                buildEnvelope * 0.55f +
                lockEnvelope * 0.85f +
                pulse * pulseEmissionAmount * 2.5f +
                rareJolt * 0.75f;

            float mainAlpha = Mathf.Clamp01(
                flickerEnvelope * Mathf.Lerp(0.15f, 0.9f, stepProgress) +
                buildEnvelope * Mathf.Lerp(0.08f, 0.82f, stepProgress) +
                lockEnvelope * Mathf.Lerp(0.55f, 1f, stepProgress) +
                pulse * pulseEmissionAmount +
                rareJolt * 0.2f
            );

            float ghostAlpha = Mathf.Clamp01(
                flickerEnvelope * Mathf.Lerp(0.25f, 1f, stepProgress) +
                lockEnvelope * 0.12f +
                pulse * 0.25f
            );

            Vector2 jitter = new Vector2(
                (Mathf.PerlinNoise(Time.time * 31f, 5.1f) - 0.5f) * logoJitterPixels * (0.1f + flickerEnvelope * 2.6f + rareJolt * 1.1f),
                (Mathf.PerlinNoise(7.2f, Time.time * 28f) - 0.5f) * logoJitterPixels * 0.55f * (0.1f + flickerEnvelope * 2.6f + rareJolt * 1.1f)
            );

            float chromaReturn = Mathf.Lerp(0.35f, 1f, holdEnvelope);
            float chromaticPulse = (flickerEnvelope > 0.01f || rareJolt > 0f)
                ? 1f + pulse * pulseChromaticAmount * 1.1f
                : 1f + pulse * pulseChromaticAmount * chromaReturn;

            if (logoMain != null)
            {
                float whiteOffsetScale = Mathf.Lerp(0.25f, 0.06f, holdEnvelope);
                logoMain.rectTransform.anchoredPosition = logoBasePosition + jitter * whiteOffsetScale;
                logoMain.rectTransform.localScale = logoBaseScale * (1f + glowBoost * 0.012f + pulse * pulseGlowAmount);
                logoMain.color = new Color(glowBoost, glowBoost, glowBoost, mainAlpha);
            }

            if (logoGhostCyan != null)
            {
                float chromaticShift = chromaticOffsetPixels * Mathf.Lerp(0.72f, 1f, 1f - holdEnvelope) * (0.12f + flickerEnvelope * 0.95f) * chromaticPulse;
                logoGhostCyan.rectTransform.anchoredPosition = logoCyanBasePosition + jitter + new Vector2(-chromaticShift, chromaticShift * 0.12f);
                logoGhostCyan.rectTransform.localScale = logoBaseScale * (1f + flickerEnvelope * 0.022f + pulse * pulseGlowAmount * 0.6f);
                logoGhostCyan.color = new Color(0.35f * glowBoost, glowBoost, glowBoost, ghostAlpha);
            }

            if (logoGhostMagenta != null)
            {
                float chromaticShift = chromaticOffsetPixels * Mathf.Lerp(0.72f, 1f, 1f - holdEnvelope) * (0.12f + flickerEnvelope * 0.95f) * chromaticPulse;
                logoGhostMagenta.rectTransform.anchoredPosition = logoMagentaBasePosition + jitter + new Vector2(chromaticShift, -chromaticShift * 0.1f);
                logoGhostMagenta.rectTransform.localScale = logoBaseScale * (1f + flickerEnvelope * 0.022f + pulse * pulseGlowAmount * 0.6f);
                logoGhostMagenta.color = new Color(glowBoost, 0.42f * glowBoost, 0.92f * glowBoost, ghostAlpha);
            }

            if (logoStackRect != null)
            {
                float breathing = 1f +
                    Mathf.Sin(Time.time * (Mathf.PI * 2f / Mathf.Max(0.001f, pulsePeriod))) * pulseGlowAmount * holdEnvelope +
                    lockEnvelope * 0.02f +
                    flickerEnvelope * 0.01f;
                logoStackRect.localScale = logoBaseScale * breathing;
            }

            if (screenBackdrop != null)
                screenBackdrop.color = Color.black;

            if (titlePlate != null)
                SetImageAlpha(titlePlate, 0f);

            if (titleArtwork != null)
                SetImageAlpha(titleArtwork, 0f);

            if (menuBackdropImage != null)
                SetImageAlpha(menuBackdropImage, 0f);

            yield return null;
        }

        yield return FadeLogoOut();
    }

    private IEnumerator WaitForBeatDrop()
    {
        if (musicSource == null || !musicSource.isPlaying)
            yield break;

        while (musicSource.time < beatDropTime)
            yield return null;
    }

    private IEnumerator PlayTitleReveal()
    {
        if (menuBackdropImage != null)
        {
            menuBackdropImage.enabled = true;
            yield return FadeGraphic(menuBackdropImage, 0f, 1f, 0.55f);
        }

        if (titlePlateRect != null)
        {
            Vector3 from = titlePlateBaseScale * 0.2f;
            Vector3 to = titlePlateBaseScale;
            titlePlateRect.localScale = from;
            titlePlateRect.anchoredPosition = titlePlateBasePosition;
            yield return ScaleTo(titlePlateRect, from, to, titleBuildLength, true);
        }

        if (titlePlate != null)
            yield return FadeGraphic(titlePlate, 0f, 1f, 0.45f);

        if (titleArtwork != null)
        {
            Vector3 from = titleArtworkBaseScale * 0.88f;
            Vector3 to = titleArtworkBaseScale;
            titleArtwork.rectTransform.localScale = from;
            titleArtwork.rectTransform.anchoredPosition = titleArtworkBasePosition;
            yield return ScaleTo(titleArtwork.rectTransform, from, to, titleBuildLength, true);
        }

        yield return PlaySubtitleReveal();

        float elapsed = 0f;
        while (elapsed < titleHoldLength)
        {
            elapsed += Time.deltaTime;
            float breathe = 1f + Mathf.Sin(Time.time * 2.6f) * titlePulseAmount + Mathf.PerlinNoise(Time.time * 4.2f, 1.1f) * titlePulseAmount * 0.5f;

            if (titleArtwork != null)
            {
                Color color = titleArtwork.color;
                color.a = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(elapsed / titleHoldLength)) * breathe;
                titleArtwork.color = color;
            }

            if (titlePlate != null)
            {
                Color color = titlePlate.color;
                color.a = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(elapsed / titleHoldLength));
                titlePlate.color = color;
            }

            if (subtitleText != null)
                ApplySubtitleLiveEffects(Mathf.Clamp01(elapsed / titleHoldLength));

            yield return null;
        }
    }

    private IEnumerator PlaySubtitleReveal()
    {
        if (subtitleText == null)
            yield break;

        if (!subtitleText.gameObject.activeSelf)
            subtitleText.gameObject.SetActive(true);

        subtitleText.enabled = true;
        subtitleText.alpha = 0f;
        subtitleText.rectTransform.anchoredPosition = subtitleBasePosition + new Vector2(-subtitleGlitchJitterPixels * 1.5f, 0f);
        subtitleText.rectTransform.localScale = subtitleBaseScale * 1.08f;

        float elapsed = 0f;
        float glitchTimer = 0f;
        float nextGlitchAt = Random.Range(0.04f, 0.22f);

        while (elapsed < subtitleEntranceLength)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextGlitchAt)
            {
                glitchTimer = Random.Range(0.04f, 0.12f);
                nextGlitchAt = elapsed + Random.Range(0.05f, 0.25f);
            }

            if (glitchTimer > 0f)
            {
                glitchTimer -= Time.deltaTime;
            }

            float t = Mathf.Clamp01(elapsed / subtitleEntranceLength);
            float reveal = Mathf.SmoothStep(0f, 1f, t);
            float flicker = Mathf.Clamp01(Mathf.PerlinNoise(Time.time * 24f, 2.4f) * 1.2f - 0.18f);
            float burst = glitchTimer > 0f ? Mathf.Clamp01(glitchTimer / 0.12f) : 0f;
            float chroma = Mathf.Clamp01(reveal * 0.8f + burst * 0.9f + flicker * 0.35f);
            float alpha = Mathf.Clamp01(reveal * 0.55f + burst * 0.55f + flicker * 0.2f);

            Vector2 jitter = new Vector2(
                (Mathf.PerlinNoise(Time.time * 33f, 8.7f) - 0.5f) * subtitleGlitchJitterPixels * (0.25f + burst * 1.5f + reveal * 0.65f),
                (Mathf.PerlinNoise(4.1f, Time.time * 27f) - 0.5f) * subtitleGlitchJitterPixels * 0.4f * (0.25f + burst * 1.5f + reveal * 0.65f)
            );

            subtitleText.rectTransform.anchoredPosition = subtitleBasePosition + jitter;
            subtitleText.rectTransform.localScale = subtitleBaseScale * (1f + reveal * 0.02f + burst * 0.03f);

            Color cyan = new Color(0.3f, 0.95f, 1f, alpha);
            Color magenta = new Color(1f, 0.4f, 0.95f, alpha);
            Color white = Color.Lerp(cyan, magenta, Mathf.Repeat(Time.time * 7.5f, 1f));
            Color color = Color.Lerp(white, Color.white, reveal * 0.55f);
            color.r *= 1f + chroma * 0.08f;
            color.g *= 1f + chroma * 0.03f;
            color.b *= 1f + chroma * 0.08f;
            subtitleText.color = color;
            subtitleText.alpha = alpha;

            yield return null;
        }

        subtitleText.alpha = 1f;
        subtitleText.rectTransform.anchoredPosition = subtitleBasePosition;
        subtitleText.rectTransform.localScale = subtitleBaseScale;
        ApplySubtitleLiveEffects(1f);
    }

    private IEnumerator PlayMenuReveal()
    {
        if (menuGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < menuRevealLength)
            {
                elapsed += Time.deltaTime;
                menuGroup.alpha = Mathf.Clamp01(elapsed / menuRevealLength);
                yield return null;
            }
        }

        SetButtonsInteractable(true);

        if (subtitleText != null)
            ApplySubtitleLiveEffects(1f);
    }

    private void ApplySubtitleLiveEffects(float lifeT)
    {
        if (subtitleText == null || !subtitleText.enabled)
            return;

        float pulsePeriod = 60f / Mathf.Max(1f, pulseBpm);
        float pulseWave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.001f, pulsePeriod));
        float pulse = pulseWave * pulseWave * subtitlePulseAmount * Mathf.Lerp(0.35f, 1f, lifeT);
        float glitchChance = Mathf.Lerp(subtitleGlitchChance * 0.7f, subtitleGlitchChance, lifeT);
        float glitchNoise = Mathf.PerlinNoise(Time.time * 7.2f, 1.7f);

        if (glitchNoise > 1f - glitchChance)
        {
            Vector2 glitchShift = new Vector2(
                (Mathf.PerlinNoise(Time.time * 51f, 3.3f) - 0.5f) * subtitleChromaticShiftPixels,
                (Mathf.PerlinNoise(5.7f, Time.time * 47f) - 0.5f) * subtitleChromaticShiftPixels * 0.35f
            );

            subtitleText.rectTransform.anchoredPosition = subtitleBasePosition + glitchShift * 0.3f;

            Color glitchColor = Mathf.Repeat(Time.time * 9f, 1f) < 0.5f
                ? new Color(0.35f, 1f, 1f, subtitleText.alpha)
                : new Color(1f, 0.45f, 0.95f, subtitleText.alpha);

            subtitleText.color = glitchColor;
        }
        else
        {
            subtitleText.rectTransform.anchoredPosition = subtitleBasePosition;

            Color color = subtitleText.color;
            float glow = 1f + pulse + Mathf.PerlinNoise(Time.time * 4.1f, 9.7f) * subtitlePulseAmount * 0.5f;
            color.r = Mathf.Clamp01(color.r * glow);
            color.g = Mathf.Clamp01(color.g * glow);
            color.b = Mathf.Clamp01(color.b * glow);
            subtitleText.color = color;
        }

        subtitleText.rectTransform.localScale = subtitleBaseScale * (1f + pulse + Mathf.PerlinNoise(Time.time * 3.5f, 6.4f) * subtitlePulseAmount * 0.35f);
    }

    private IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null)
            yield break;

        Color color = graphic.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(from, to, t);
            graphic.color = color;
            yield return null;
        }

        color.a = to;
        graphic.color = color;
    }

    private IEnumerator FadeLogoOut()
    {
        float elapsed = 0f;
        float duration = logoFadeOutLength;

        Color mainColor = logoMain != null ? logoMain.color : Color.black;
        Color cyanColor = logoGhostCyan != null ? logoGhostCyan.color : Color.black;
        Color magentaColor = logoGhostMagenta != null ? logoGhostMagenta.color : Color.black;
        float mainAlpha = mainColor.a;
        float cyanAlpha = cyanColor.a;
        float magentaAlpha = magentaColor.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = Mathf.SmoothStep(1f, 0f, t);

            if (logoMain != null)
            {
                mainColor.a = mainAlpha * fade;
                logoMain.color = mainColor;
            }

            if (logoGhostCyan != null)
            {
                cyanColor.a = cyanAlpha * fade;
                logoGhostCyan.color = cyanColor;
            }

            if (logoGhostMagenta != null)
            {
                magentaColor.a = magentaAlpha * fade;
                logoGhostMagenta.color = magentaColor;
            }

            yield return null;
        }

        SetLogoGraphicState(logoMain, Color.black, 0f);
        SetLogoGraphicState(logoGhostCyan, Color.black, 0f);
        SetLogoGraphicState(logoGhostMagenta, Color.black, 0f);
    }

    private IEnumerator ScaleTo(RectTransform target, Vector3 from, Vector3 to, float duration, bool overshoot)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = overshoot ? Mathf.SmoothStep(0f, 1f, t) : t;

            if (overshoot)
            {
                float pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                target.localScale = Vector3.Lerp(from, to, eased) * pop;
            }
            else
            {
                target.localScale = Vector3.Lerp(from, to, eased);
            }

            yield return null;
        }

        target.localScale = to;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetLogoGraphicState(Graphic graphic, Color color, float alpha)
    {
        if (graphic == null)
            return;

        Color targetColor = color;
        targetColor.a = alpha;
        graphic.color = targetColor;
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (menuButtons != null)
        {
            foreach (Button button in menuButtons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        if (neonButtons != null)
        {
            foreach (NeonUIButtonAnimator neonButton in neonButtons)
            {
                if (neonButton != null)
                    neonButton.SetActive(interactable);
            }
        }
    }

    private void SkipToMenu()
    {
        if (sequenceStarted)
        {
            StopAllCoroutines();
            sequenceStarted = false;
        }

        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        if (menuGroup != null)
            menuGroup.alpha = 1f;

        SetButtonsInteractable(true);
    }

    public void StartGame()
    {
        if (!string.IsNullOrWhiteSpace(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }
}
