using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string isSprintingParameter = "IsSprinting";

    private Animator animator;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private Vector2 facingDirection = Vector2.down;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Vector2 movement = playerMovement.GetMovementDirection();
        bool isMoving = movement.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            facingDirection = movement;
        }

        // Flip sprite horizontally when moving left/right
        if (spriteRenderer != null)
        {
            // Use facingDirection so sprite faces last non-zero horizontal input
            spriteRenderer.flipX = facingDirection.x < 0f;
        }

        animator.SetFloat(speedParameter, movement.magnitude);
        animator.SetFloat(moveXParameter, facingDirection.x);
        animator.SetFloat(moveYParameter, facingDirection.y);
        animator.SetBool(isMovingParameter, isMoving);
        animator.SetBool(isSprintingParameter, playerMovement.GetCurrentSpeedMultiplier() > 1f);
    }
}