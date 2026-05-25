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
    [SerializeField] private string shootTriggerParameter = "Shoot";

    private Animator animator;
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;
    private SpriteRenderer spriteRenderer;
    private Vector2 facingDirection = Vector2.down;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (playerShooter != null)
        {
            playerShooter.OnShotFired += HandleShotFired;
        }
    }

    private void OnDisable()
    {
        if (playerShooter != null)
        {
            playerShooter.OnShotFired -= HandleShotFired;
        }
    }

    private void Update()
    {
        Vector2 movement = playerMovement.GetMovementDirection();
        bool isMoving = movement.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            facingDirection = movement;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection.x < 0f;
        }

        animator.SetFloat(speedParameter, movement.magnitude);
        animator.SetFloat(moveXParameter, facingDirection.x);
        animator.SetFloat(moveYParameter, facingDirection.y);
        animator.SetBool(isMovingParameter, isMoving);
        animator.SetBool(isSprintingParameter, playerMovement.GetCurrentSpeedMultiplier() > 1f);
    }

    private void HandleShotFired()
    {
        animator.SetTrigger(shootTriggerParameter);
    }
}