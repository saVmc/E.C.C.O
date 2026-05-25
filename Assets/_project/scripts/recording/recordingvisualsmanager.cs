using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class RecordingVisualsManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private Text timerText;
    [SerializeField] private TextMeshProUGUI timerTextTMP;
    [SerializeField] private float overlayFadeDuration = 0.2f;
    [SerializeField] private float overlayAlphaTarget = 0.3f;
    [SerializeField] private Transform playerTransform; // for a glow
    [SerializeField] private SpriteRenderer playerSpriteRenderer; // ""
    [SerializeField] private ParticleSystem pixelFireTrailPrefab;
    [SerializeField] private ParticleSystem deathExplosionPrefab;
    [SerializeField] private float maxEmissionRate = 10f; // when speedy
    [SerializeField] private float minEmissionRate = 2f; 
    private RecordingManager recordingManager;
    private PlayerMovement playerMovement;
    private float overlayFadeTimer = 0f;
    private bool overlayFading = false;
    private bool isRecording = false;
    private Color originalSpriteColor;
    private float glowTimer = 0f;
    private ParticleSystem activePixelTrail;     private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        recordingManager = RecordingManager.Instance;

        if (overlayCanvasGroup == null)
            overlayCanvasGroup = GetComponentInChildren<CanvasGroup>();
        if (timerText == null)
            timerText = GetComponentInChildren<Text>();
        if (timerTextTMP == null)
            timerTextTMP = GetComponentInChildren<TextMeshProUGUI>();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
        }

        if (playerTransform == null)
            playerTransform = FindAnyObjectByType<PlayerMovement>()?.GetComponent<Transform>();

        if (playerSpriteRenderer == null && playerTransform != null)
            playerSpriteRenderer = playerTransform.GetComponent<SpriteRenderer>();

        if (playerTransform != null)
            playerMovement = playerTransform.GetComponent<PlayerMovement>();

        if (playerSpriteRenderer != null)
            originalSpriteColor = playerSpriteRenderer.color;


    }

    private void OnEnable()
    {
        if (recordingManager != null)
        {
            recordingManager.OnRecordingStarted += HandleRecordingStarted;
            recordingManager.OnRecordingStopped += HandleRecordingStopped;
            recordingManager.OnTimeUpdated += HandleTimeUpdated;
        }
    }

    private void OnDisable()
    {
        if (recordingManager != null)
        {
            recordingManager.OnRecordingStarted -= HandleRecordingStarted;
            recordingManager.OnRecordingStopped -= HandleRecordingStopped;
            recordingManager.OnTimeUpdated -= HandleTimeUpdated;
        }
    }

    private void Update()
    {
        if (overlayFading && overlayCanvasGroup != null)
        {
            overlayFadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(overlayFadeTimer / overlayFadeDuration);
            overlayCanvasGroup.alpha = Mathf.Lerp(overlayCanvasGroup.alpha, overlayAlphaTarget, t);

            if (t >= 1f)
                overlayFading = false;
        }

        if (isRecording && playerSpriteRenderer != null)
        {
            glowTimer += Time.deltaTime;
            float glowIntensity = Mathf.Sin(glowTimer * Mathf.PI * 0.5f) * 0.15f; 
            Color glowColor = originalSpriteColor;
            glowColor.r = Mathf.Clamp01(originalSpriteColor.r + glowIntensity);
            glowColor.g = Mathf.Clamp01(originalSpriteColor.g + glowIntensity);
            glowColor.b = Mathf.Clamp01(originalSpriteColor.b + glowIntensity);
            playerSpriteRenderer.color = glowColor;
        }

        if (isRecording && activePixelTrail != null && playerMovement != null)
        {
            activePixelTrail.transform.position = playerTransform.position;
            
            Vector2 playerVelocity = playerMovement.GetMovementDirection();
            float speed = playerVelocity.magnitude; 
            
            float emissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, speed);
            emissionModule.rateOverTime = emissionRate;
        }
    }

    private void HandleRecordingStarted()
    {
        isRecording = true;
        glowTimer = 0f;
        overlayAlphaTarget = 0.3f;
        overlayFading = true;
        overlayFadeTimer = 0f;

        if (pixelFireTrailPrefab != null && playerTransform != null)
        {
            activePixelTrail = Instantiate(pixelFireTrailPrefab, playerTransform.position, Quaternion.identity);
            emissionModule = activePixelTrail.emission;
            emissionModule.rateOverTime = minEmissionRate; 
        }

        if (playerTransform != null && playerSpriteRenderer != null)
        {
            StartCoroutine(PixelExplosionBurst());
        }
    }

    private void HandleRecordingStopped()
    {
        isRecording = false;
        glowTimer = 0f;
        overlayAlphaTarget = 0f;
        overlayFading = true;
        overlayFadeTimer = 0f;

        if (timerTextTMP != null)
            timerTextTMP.text = "";
        else if (timerText != null)
            timerText.text = "";

        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = originalSpriteColor;

        if (activePixelTrail != null)
        {
            activePixelTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(activePixelTrail.gameObject, 1f);
            activePixelTrail = null;
        }
    }

    private void HandleTimeUpdated(float timeRemaining)
    {
        if (timerTextTMP != null)
        {
            timerTextTMP.text = Mathf.Max(0f, timeRemaining).ToString("F1") + "s";
        }
        else if (timerText != null)
        {
            timerText.text = Mathf.Max(0f, timeRemaining).ToString("F1") + "s";
        }
    }

    public void SpawnDeathExplosion(Vector3 position)
    {
        ParticleSystem explosionPrefab = deathExplosionPrefab != null ? deathExplosionPrefab : pixelFireTrailPrefab;
        if (explosionPrefab == null)
            return;

        ParticleSystem explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.Play();
        Destroy(explosion.gameObject, explosion.main.duration + explosion.main.startLifetime.constantMax);
    }


    private System.Collections.IEnumerator PixelExplosionBurst()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = playerTransform.localScale;

        if (activePixelTrail != null)
        {
            emissionModule.rateOverTime = maxEmissionRate * 2f; }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; 
            
            float easeOut = 1f - (t * t); 
            float maxScale = 1.15f; 
            float currentScale = 1f + (maxScale - 1f) * easeOut;
            playerTransform.localScale = originalScale * currentScale;

            if (playerSpriteRenderer != null)
            {
                Color explosionColor = originalSpriteColor;
                explosionColor.r = Mathf.Clamp01(originalSpriteColor.r + (1f - t) * 0.3f);
                explosionColor.g = Mathf.Clamp01(originalSpriteColor.g + (1f - t) * 0.3f);
                explosionColor.b = Mathf.Clamp01(originalSpriteColor.b + (1f - t) * 0.3f);
                playerSpriteRenderer.color = explosionColor;
            }

            yield return null;
        }

        // Reset
        if (playerTransform != null)
            playerTransform.localScale = originalScale;
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = originalSpriteColor;
    }
}
