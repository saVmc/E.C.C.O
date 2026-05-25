using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    private bool isSprinting;
    private Vector2 lastRecordedInput;

    public event Action<Vector2> OnMovementInput;
    public event Action<bool> OnSprintToggled;

    private void Awake()
    {
        if (GetComponent<TimeParadoxDeathController>() == null)
            gameObject.AddComponent<TimeParadoxDeathController>();

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            moveInput.y -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveInput.x += 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveInput.x -= 1;

        moveInput = moveInput.normalized;

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            facingDirection = moveInput;
        }

        if (moveInput != lastRecordedInput)
        {
            OnMovementInput?.Invoke(moveInput);
            lastRecordedInput = moveInput;
        }
        bool wasSprinting = isSprinting;
        isSprinting = Keyboard.current.leftShiftKey.isPressed;
        if (isSprinting != wasSprinting)
            OnSprintToggled?.Invoke(isSprinting);
    }

    private void FixedUpdate()
    {
        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        rb.linearVelocity = moveInput * currentSpeed;
    }
    public Vector3 GetPosition() => transform.position;
    public Vector2 GetMovementDirection() => moveInput;
    public Vector2 GetFacingDirection() => facingDirection;

    public float GetCurrentSpeedMultiplier() => isSprinting ? sprintMultiplier : 1f;
    public bool GetIsSprinting() => isSprinting;
}