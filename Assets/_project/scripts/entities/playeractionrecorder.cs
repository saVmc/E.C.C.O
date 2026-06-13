using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Records player actions (movement, sprint, shooting) for replay.
/// Attach to the player entity that also has PlayerMovement and PlayerShooter components.
/// </summary>
public class PlayerActionRecorder : MonoBehaviour
{
    private PlayerMovement movement;
    private PlayerShooter shooter;
    private List<PlayerAction> actions = new();
    private bool isRecording;
    private float recordTime;

    public struct PlayerAction
    {
        public float timestamp;
        public Vector2 position;
        public Vector2 movementDirection;
        public bool isSprinting;
        public bool didShoot;
        public Vector2 shootDirection;
    }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        shooter = GetComponent<PlayerShooter>();
    }

    private void OnEnable()
    {
        if (movement != null) movement.OnMovementInput += RecordMovement;
        if (movement != null) movement.OnSprintToggled += RecordSprint;
        if (shooter != null) shooter.OnShotFired += RecordShot;
    }

    private void OnDisable()
    {
        if (movement != null) movement.OnMovementInput -= RecordMovement;
        if (movement != null) movement.OnSprintToggled -= RecordSprint;
        if (shooter != null) shooter.OnShotFired -= RecordShot;
    }

    private void RecordMovement(Vector2 dir)
    {
        if (!isRecording) return;

        AddAction(new PlayerAction
        {
            timestamp = recordTime,
            position = movement.GetPosition(),
            movementDirection = dir,
            isSprinting = movement.GetIsSprinting(),
            didShoot = false,
            shootDirection = Vector2.zero
        });
    }

    private void RecordSprint(bool sprinting)
    {
        if (!isRecording) return;

        AddAction(new PlayerAction
        {
            timestamp = recordTime,
            position = movement.GetPosition(),
            movementDirection = movement.GetMovementDirection(),
            isSprinting = sprinting,
            didShoot = false,
            shootDirection = Vector2.zero
        });
    }

    private void RecordShot()
    {
        if (!isRecording) return;

        // Grab the current active gun and its aim direction.
        // GetActiveGun() is the method exposed by PlayerShooter that returns the active Gun.
        Vector2 aimDirection = Vector2.right; // fallback if no gun is assigned
        if (shooter != null && shooter.GetActiveGun() != null)
        {
            // The gun's GetAimDirection() now returns the raw, un‑normalised vector,
            // preserving distance information for the flip logic.
            aimDirection = shooter.GetActiveGun().GetAimDirection();
        }

        AddAction(new PlayerAction
        {
            timestamp = recordTime,
            position = movement.GetPosition(),
            movementDirection = movement.GetMovementDirection(),
            isSprinting = movement.GetIsSprinting(),
            didShoot = true,
            shootDirection = aimDirection
        });
    }

    private void AddAction(PlayerAction a) => actions.Add(a);

    private void Update()
    {
        if (isRecording) recordTime += Time.deltaTime;
    }

    public void StartRecording()
    {
        actions.Clear();
        recordTime = 0f;
        isRecording = true;
        // Capture the initial state immediately.
        RecordCurrentState();
    }

    public void StopRecording()
    {
        if (isRecording) RecordCurrentState();
        isRecording = false;
    }

    /// <summary>
    /// Takes a snapshot of the current state (position, movement, sprint, and whether a shot just occurred).
    /// </summary>
    private void RecordCurrentState()
    {
        var act = new PlayerAction
        {
            timestamp = recordTime,
            position = movement.GetPosition(),
            movementDirection = movement.GetMovementDirection(),
            isSprinting = movement.GetIsSprinting(),
            didShoot = false,
            shootDirection = Vector2.zero
        };
        actions.Add(act);
    }

    public List<PlayerAction> GetRecordedActions() => new(actions);
    public void ClearRecording() => actions.Clear();
    public bool IsRecording => isRecording;
    public int GetActionCount() => actions.Count;
    public float RecordingDuration => recordTime;
}