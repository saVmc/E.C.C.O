using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private PlayerInputActions input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Gameplay.Move.performed += OnMove;
        input.Gameplay.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        input.Gameplay.Move.performed -= OnMove;
        input.Gameplay.Move.canceled -= OnMove;
        input.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}