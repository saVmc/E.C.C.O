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
    private Vector2 facingDirection = Vector2.down;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        Vector2 movement = playerMovement.GetMovementDirection();
        bool isMoving = movement.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            facingDirection = movement;
        }

        animator.SetFloat(speedParameter, movement.magnitude);
        animator.SetFloat(moveXParameter, facingDirection.x);
        animator.SetFloat(moveYParameter, facingDirection.y);
        animator.SetBool(isMovingParameter, isMoving);
        animator.SetBool(isSprintingParameter, playerMovement.GetCurrentSpeedMultiplier() > 1f);
    }
}