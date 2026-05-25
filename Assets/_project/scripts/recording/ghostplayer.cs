using System.Collections.Generic;
using UnityEngine;
using PlayerAction = PlayerActionRecorder.PlayerAction;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GhostPlayer : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private int projectileDamage = 1;

    private List<PlayerAction> recordedActions;
    private RecordingVisualsManager visualsManager;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerShooter playerShooter; 

    private int currentActionIndex = 0;
    private float playbackElapsedTime = 0f;
    private bool isPlaying = false;
    private HashSet<int> firedAtIndices = new HashSet<int>(); 
    private HashSet<int> animatedShotIndices = new HashSet<int>(); 
    private Vector3 originalScale;

    private void Awake()
    {
        if (GetComponent<TimeParadoxDeathController>() == null)
            gameObject.AddComponent<TimeParadoxDeathController>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
            
        rb = GetComponent<Rigidbody2D>();
    }

    public void PlayRecording(List<PlayerAction> actions)
    {
        if (actions == null || actions.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        recordedActions = actions;
        
        if (visualsManager == null)
            visualsManager = FindAnyObjectByType<RecordingVisualsManager>();

        if (projectilePrefab == null)
        {
            playerShooter = FindAnyObjectByType<PlayerShooter>();
            if (playerShooter != null)
            {
                projectilePrefab = playerShooter.GetProjectilePrefab();
            }
        }
        else
        {
            playerShooter = FindAnyObjectByType<PlayerShooter>();
        }

        isPlaying = true;
        currentActionIndex = 0;
        playbackElapsedTime = 0f;
        firedAtIndices.Clear();
        animatedShotIndices.Clear();
        originalScale = transform.localScale;

        if (recordedActions.Count > 0)
            transform.position = recordedActions[0].position;

    }


    private void Update()
    {
        if (!isPlaying || recordedActions == null || recordedActions.Count == 0)
            return;

        playbackElapsedTime += Time.deltaTime;

        if (recordedActions.Count == 1)
        {
            PlayerAction action = recordedActions[0];
            transform.position = action.position;
            ApplyAnimation(action);
            
            if (action.didShoot && projectilePrefab != null && !firedAtIndices.Contains(0))
            {
                ReplayShot(action);
                firedAtIndices.Add(0);
            }

            if (playbackElapsedTime > 0.5f)
            {
                isPlaying = false;
                Destroy(gameObject);
            }
            return;
        }

        while (currentActionIndex < recordedActions.Count - 1 && 
                playbackElapsedTime >= recordedActions[currentActionIndex + 1].timestamp)
        {
            currentActionIndex++;
        }

        if (currentActionIndex >= recordedActions.Count - 1 && 
            playbackElapsedTime >= recordedActions[recordedActions.Count - 1].timestamp + 0.5f)
        {
            isPlaying = false;
            StartCoroutine(FadeOutEffect());
            return;
        }

        PlayerAction currentAction = recordedActions[currentActionIndex];
        PlayerAction nextAction = (currentActionIndex + 1 < recordedActions.Count) 
            ? recordedActions[currentActionIndex + 1] 
            : currentAction;

        float actionDuration = nextAction.timestamp - currentAction.timestamp;
        if (actionDuration < 0.001f)
            actionDuration = 0.1f; 

        float timeSinceCurrentAction = playbackElapsedTime - currentAction.timestamp;
        float t = Mathf.Clamp01(timeSinceCurrentAction / actionDuration);
        
        Vector3 interpolatedPos = Vector3.Lerp(currentAction.position, nextAction.position, t);
        transform.position = interpolatedPos;

        ApplyAnimation(currentAction);

        if (currentAction.didShoot && projectilePrefab != null && !firedAtIndices.Contains(currentActionIndex))
        {
            ReplayShot(currentAction);
            firedAtIndices.Add(currentActionIndex);
        }
    }

    private void ApplyAnimation(PlayerAction action)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", action.movementDirection.magnitude);
            animator.SetFloat("MoveX", action.movementDirection.x);
            animator.SetFloat("MoveY", action.movementDirection.y);
            animator.SetBool("IsMoving", action.movementDirection.sqrMagnitude > 0.0001f);
            animator.SetBool("IsSprinting", action.isSprinting);

            if (spriteRenderer != null && action.movementDirection.x != 0f)
                spriteRenderer.flipX = action.movementDirection.x < 0f;
        }
    }


    private void ReplayShot(PlayerAction action)
    {
        if (projectilePrefab == null)
            return;

        Vector2 shootDirection = action.shootDirection;

        Projectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.Initialize(shootDirection, projectileSpeed, projectileLifetime, projectileDamage, gameObject);

        SpriteRenderer projectileSprite = projectile.GetComponent<SpriteRenderer>();
        if (projectileSprite != null && spriteRenderer != null)
        {
            projectileSprite.color = spriteRenderer.color;
        }

        if (projectile != null)
        {
            projectile.transform.rotation = Quaternion.identity;
            projectile.transform.right = shootDirection;
            
            if (projectileSprite != null)
                projectileSprite.flipX = shootDirection.x < 0f;
        }

        Transform gun = transform.Find("Gun");
        if (gun != null)
        {
            Transform muzzleLight = gun.Find("MuzzleLight");
            if (muzzleLight != null)
            {
                float muzzleX = shootDirection.x < 0f ? -0.6f : 0.6f;
                Vector3 pos = muzzleLight.localPosition;
                pos.x = muzzleX;
                muzzleLight.localPosition = pos;
            }
        }

        if (animator != null && !animatedShotIndices.Contains(currentActionIndex))
        {
            animator.SetTrigger("Shoot");
            animatedShotIndices.Add(currentActionIndex);
        }
    }

    public void StopPlayback()
    {
        isPlaying = false;
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator FadeOutEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 0 to 1 :)

            float easeIn = t * t;
            float shrinkScale = 1f - (0.2f * easeIn);
            transform.localScale = originalScale * shrinkScale;

            yield return null;
        }

        Destroy(gameObject);
    }
}
