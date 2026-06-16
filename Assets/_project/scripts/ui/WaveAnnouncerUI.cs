using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WaveAnnouncerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.6f;

    private void Start()
    {
        if (waveText != null) waveText.gameObject.SetActive(false);

        if (HordeSpawner.Instance != null)
        {
            HordeSpawner.Instance.OnWaveStarted   += OnWaveStarted;
            HordeSpawner.Instance.OnWaveCompleted += OnWaveCompleted;
            HordeSpawner.Instance.OnBossSpawned   += OnBossSpawned;
        }
    }

    private void OnDestroy()
    {
        if (HordeSpawner.Instance != null)
        {
            HordeSpawner.Instance.OnWaveStarted   -= OnWaveStarted;
            HordeSpawner.Instance.OnWaveCompleted -= OnWaveCompleted;
            HordeSpawner.Instance.OnBossSpawned   -= OnBossSpawned;
        }
    }

    private void OnWaveStarted(int wave)
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(AnnounceRoutine($"WAVE {wave}"));
    }

    private void OnWaveCompleted(int wave)
    {
        // No announcement on wave clear — next wave start announces the new number
    }

    private void OnBossSpawned(int bossNumber)
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(AnnounceRoutine("BOSS SPAWNED!"));
    }

    private IEnumerator AnnounceRoutine(string message)
    {
        if (waveText == null) yield break;

        waveText.text = message;
        waveText.alpha = 1f;
        waveText.gameObject.SetActive(true);

        yield return new WaitForSeconds(holdDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            waveText.alpha = 1f - t / fadeDuration;
            yield return null;
        }

        waveText.gameObject.SetActive(false);
    }
}
