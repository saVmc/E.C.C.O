using UnityEngine;
using System.Collections.Generic;

public class PlayerActionRecorder : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private List<PlayerAction> recordedActions = new();
    private bool isRecording;
    private float recordingTime;

    [System.Serializable]
    public struct PlayerAction
    {
        public float timestamp;
        public Vector2 position;
        public Vector2 movementDirection;
        public bool isSprinting;
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerActionRecorder requires PlayerMovement component on the same GameObject!");
        }
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovementInput += RecordMovement;
            playerMovement.OnSprintToggled += RecordSprint;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovementInput -= RecordMovement;
            playerMovement.OnSprintToggled -= RecordSprint;
        }
    }

    private void Update()
    {
        if (isRecording)
        {
            recordingTime += Time.deltaTime;
        }
    }

    public void StartRecording()
    {
        recordedActions.Clear();
        recordingTime = 0f;
        isRecording = true;
        Debug.Log("Recording started.");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"Recording stopped. Recorded {recordedActions.Count} actions in {recordingTime:F2} seconds.");
    }

    private void RecordMovement(Vector2 direction)
    {
        if (!isRecording) return;

        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = direction,
            isSprinting = playerMovement.GetCurrentSpeedMultiplier() > 1f
        };

        recordedActions.Add(action);
    }

    private void RecordSprint(bool sprinting)
    {
        if (!isRecording) return;

        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = playerMovement.GetMovementDirection(),
            isSprinting = sprinting
        };

        recordedActions.Add(action);
    }

    public List<PlayerAction> GetRecordedActions() => new(recordedActions);

    public void ClearRecording()
    {
        recordedActions.Clear();
        recordingTime = 0f;
    }
    public bool IsRecording => isRecording;

    public float RecordingDuration => recordingTime;
}
