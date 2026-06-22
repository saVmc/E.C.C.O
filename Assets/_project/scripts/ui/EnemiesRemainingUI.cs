using TMPro;
using UnityEngine;

public sealed class EnemiesRemainingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (label == null) return;

        if (HordeSpawner.Instance == null || !HordeSpawner.Instance.IsActive)
        {
            label.text = "";
            return;
        }

        int wave      = HordeSpawner.Instance.CurrentWave;
        int remaining = HordeSpawner.Instance.WaveEnemiesRemaining;

        label.text = remaining > 0
            ? $"WAVE {wave}\n{remaining} ENEMIES LEFT"
            : $"WAVE {wave}\nWAVE CLEAR";
    }
}
