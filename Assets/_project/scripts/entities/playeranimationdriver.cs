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
    private bool aimFlipOverride = false;
    private bool aimFlipValue = false;

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
            facingDirection = movement;

        if (spriteRenderer != null)
            spriteRenderer.flipX = aimFlipOverride ? aimFlipValue : facingDirection.x < 0f;

        animator.SetFloat(speedParameter, movement.magnitude);
        animator.SetFloat(moveXParameter, facingDirection.x);
        animator.SetFloat(moveYParameter, facingDirection.y);
        animator.SetBool(isMovingParameter, isMoving);
        animator.SetBool(isSprintingParameter, playerMovement.GetCurrentSpeedMultiplier() > 1f);
    }

    public void SetAimFlip(bool flipped)
    {
        aimFlipOverride = true;
        aimFlipValue = flipped;
    }
}