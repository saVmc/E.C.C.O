using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PlayerActionRecorder: Captures player input actions for recording and playback.
/// 
/// OOP Design:
/// - Single Responsibility: Records all player actions (movement, sprint, shooting) to a list
/// - Dependency Injection: Listens to PlayerMovement and PlayerShooter events
/// - Encapsulation: Internal list of actions; exposes only copy via GetRecordedActions()
/// - Extensibility: PlayerAction struct can be extended with new action types without changing core logic
/// 
/// Event-Driven Architecture:
/// - Subscribes to PlayerMovement.OnMovementInput and OnSprintToggled
/// - Subscribes to PlayerShooter.OnShotFired
/// - Records timestamp, position, direction, sprint state, and shooting flag
/// 
/// Data Structure:
/// - List<PlayerAction> stores all captured actions chronologically
/// - Each action contains: timestamp, position, movementDirection, isSprinting, didShoot, shootDirection
/// </summary>
public class PlayerActionRecorder : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;
    private List<PlayerAction> recordedActions = new();
    private bool isRecording;
    private float recordingTime;
    private Vector2 currentMovementDirection = Vector2.zero;
    private bool currentIsSprinting = false;

    /// <summary>
    /// Immutable struct representing a snapshot of player state at a given time.
    /// Includes movement, sprint state, position, and shooting flag.
    /// </summary>
    [System.Serializable]
    public struct PlayerAction
    {
        /// <summary>Time (in seconds from recording start) when this action occurred.</summary>
        public float timestamp;

        /// <summary>Player world position at this moment.</summary>
        public Vector2 position;

        /// <summary>Movement direction input (normalized -1 to 1 per axis).</summary>
        public Vector2 movementDirection;

        /// <summary>Whether player was sprinting during this frame.</summary>
        public bool isSprinting;

        /// <summary>Whether player fired a projectile during this frame.</summary>
        public bool didShoot;

        /// <summary>Direction player was facing when shot fired (used for ghost playback).</summary>
        public Vector2 shootDirection;
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovementInput += RecordMovement;
            playerMovement.OnSprintToggled += RecordSprint;
        }

        if (playerShooter != null)
        {
            playerShooter.OnShotFired += RecordShot;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnMovementInput -= RecordMovement;
            playerMovement.OnSprintToggled -= RecordSprint;
        }

        if (playerShooter != null)
        {
            playerShooter.OnShotFired -= RecordShot;
        }
    }

    private void RecordMovement(Vector2 direction)
    {
        if (!isRecording) return;

        currentMovementDirection = direction;

        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = direction,
            isSprinting = currentIsSprinting,
            didShoot = false,
            shootDirection = Vector2.zero
        };

        recordedActions.Add(action);
    }
    private void RecordSprint(bool sprinting)
    {
        if (!isRecording) return;

        currentIsSprinting = sprinting;

        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = currentMovementDirection,
            isSprinting = sprinting,
            didShoot = false,
            shootDirection = Vector2.zero
        };

        recordedActions.Add(action);
    }

    private void RecordShot()
    {
        if (!isRecording) return;

        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = currentMovementDirection,
            isSprinting = currentIsSprinting,
            didShoot = true,
            shootDirection = playerMovement.GetFacingDirection()
        };

        recordedActions.Add(action);
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
        
        if (playerMovement != null)
        {
            currentMovementDirection = playerMovement.GetMovementDirection();
            currentIsSprinting = playerMovement.GetIsSprinting();
        }
        else
        {
            currentMovementDirection = Vector2.zero;
            currentIsSprinting = false;
        }

        RecordCurrentState();
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            RecordCurrentState();
        }

        isRecording = false;
    }

    private void RecordCurrentState()
    {
        var action = new PlayerAction
        {
            timestamp = recordingTime,
            position = playerMovement.GetPosition(),
            movementDirection = currentMovementDirection,
            isSprinting = currentIsSprinting,
            didShoot = false,
            shootDirection = Vector2.zero
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

    public int GetActionCount() => recordedActions.Count;

    public float RecordingDuration => recordingTime;
}
