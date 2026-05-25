using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RecordingInputHandler : MonoBehaviour
{
    [SerializeField] private GhostPlayer ghostPlayerPrefab;

    private RecordingManager recordingManager;
    private RecordingDevice recordingDevice;
    private bool recordingWasActive = false;

    private void Start()
    {
        recordingManager = RecordingManager.Instance;
        recordingDevice = recordingManager.GetDevice();
    }

    private void Update()
    {
        if (recordingDevice == null)
            return;

        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            recordingDevice.Unlock();
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (recordingManager.IsRecording)
            {
                recordingDevice.RequestStopRecording();
            }
            else
            {
                recordingDevice.RequestStartRecording();
            }
        }

        if (recordingWasActive && !recordingManager.IsRecording)
        {
            SpawnGhost();
            recordingWasActive = false;
        }

        recordingWasActive = recordingManager.IsRecording;
    }

    private void SpawnGhost()
    {
        var recorder = recordingManager.GetRecorder();
        if (recorder == null)
            return;

        var recordedActions = recorder.GetRecordedActions();
        if (recordedActions.Count == 0)
        {
            return;
        }

        float recordingDuration = recorder.RecordingDuration;
        if (recordingDuration < 0.3f)
        {
            return;
        }

        if (ghostPlayerPrefab == null)
        {
            return;
        }

        var player = FindAnyObjectByType<PlayerMovement>();
        Vector3 spawnPos = player != null ? player.GetPosition() : Vector3.zero;
        TimeParadoxDeathController playerDeath = player != null ? player.GetComponent<TimeParadoxDeathController>() : null;

        GhostPlayer ghost = Instantiate(ghostPlayerPrefab, spawnPos, Quaternion.identity);
        TimeParadoxDeathController ghostDeath = ghost.GetComponent<TimeParadoxDeathController>();

        if (playerDeath != null && ghostDeath != null)
        {
            playerDeath.SetPartner(ghostDeath);
            ghostDeath.SetPartner(playerDeath);
        }

        ghost.PlayRecording(recordedActions);
    }
}
