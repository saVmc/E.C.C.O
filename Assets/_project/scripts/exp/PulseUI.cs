using UnityEngine;

public sealed class PulseUI : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.03f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }

    public void SetPulsing(bool active)
    {
        enabled = active;
        if (!active)
            transform.localScale = originalScale;
    }
}