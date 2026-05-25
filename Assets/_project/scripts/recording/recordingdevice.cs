using System;
using UnityEngine;

public sealed class RecordingDevice : MonoBehaviour
{
    [SerializeField] private float maxRecordingTime = 10f;
    [SerializeField] private Color recordingTintColor = new Color(0f, 0.8f, 1f, 0.3f); // Cyan, 30% alpha
    
    private bool isUnlocked = false;

    public event Action OnDeviceUnlocked;
    public event Action OnRecordingStartRequested;
    public event Action OnRecordingStopRequested;

    public void Unlock()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;
        OnDeviceUnlocked?.Invoke();
    }
    public bool IsUnlocked => isUnlocked;

    public float GetMaxRecordingTime() => maxRecordingTime;

    public Color GetRecordingTintColor() => recordingTintColor;

    public void RequestStartRecording()
    {
        if (!isUnlocked)
            return;
        OnRecordingStartRequested?.Invoke();
    }
    public void RequestStopRecording()
    {
        OnRecordingStopRequested?.Invoke();
    }
}
