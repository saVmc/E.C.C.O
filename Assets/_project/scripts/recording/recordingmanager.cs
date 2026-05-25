using System;
using UnityEngine;

public sealed class RecordingManager : MonoBehaviour
{
    [SerializeField] private RecordingDevice device;
    [SerializeField] private PlayerActionRecorder actionRecorder;

    private static RecordingManager instance;

    private bool isRecording = false;
    private float recordingTimeRemaining = 0f;
    private float recordingSessionStartTime = 0f;

    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;
    public event Action<float> OnTimeUpdated;
    public event Action OnTimeExpired;

    public static RecordingManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<RecordingManager>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (device == null)
            device = GetComponent<RecordingDevice>();
        if (actionRecorder == null)
            actionRecorder = FindAnyObjectByType<PlayerActionRecorder>();

        if (device != null)
        {
            device.OnRecordingStartRequested += StartRecording;
            device.OnRecordingStopRequested += StopRecording;
        }
    }

    private void OnDestroy()
    {
        if (device != null)
        {
            device.OnRecordingStartRequested -= StartRecording;
            device.OnRecordingStopRequested -= StopRecording;
        }
    }

    private void Update()
    {
        if (!isRecording)
            return;

        recordingTimeRemaining -= Time.deltaTime;
        OnTimeUpdated?.Invoke(recordingTimeRemaining);

        if (recordingTimeRemaining <= 0f)
        {
            StopRecording();
            OnTimeExpired?.Invoke();
        }
    }

    public void StartRecording()
    {
        if (isRecording || device == null || !device.IsUnlocked)
            return;

        isRecording = true;
        recordingTimeRemaining = device.GetMaxRecordingTime();
        recordingSessionStartTime = Time.time;

        if (actionRecorder != null)
            actionRecorder.StartRecording();

        OnRecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!isRecording)
            return;

        isRecording = false;

        if (actionRecorder != null)
            actionRecorder.StopRecording();

        OnRecordingStopped?.Invoke();
    }

    public bool IsRecording => isRecording;


    public float GetTimeRemaining() => Mathf.Max(0f, recordingTimeRemaining);

    public RecordingDevice GetDevice() => device;

    public PlayerActionRecorder GetRecorder() => actionRecorder;
}
