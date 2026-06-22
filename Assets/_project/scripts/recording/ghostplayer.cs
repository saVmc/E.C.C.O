using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAction = PlayerActionRecorder.PlayerAction;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GhostPlayer : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;

    // ─── Config (set via public API before PlayRecording) ────────────────────
    private Color  ghostTint          = new Color(0.4f, 0.9f, 1f, 0.75f);
    private float  damageMultiplier   = 1f;
    private int    spreadCount        = 1;
    private float  spreadHalfAngle    = 12f;
    private int    maxLoops           = 1;
    private float  playbackSpeed      = 1f;

    // ─── Runtime state ────────────────────────────────────────────────────────
    private List<PlayerAction>       recordedActions;
    private RecordingVisualsManager  visualsManager;
    private SpriteRenderer           spriteRenderer;
    private Animator                 animator;
    private Rigidbody2D              rb;
    private PlayerShooter            playerShooter;

    private int           currentActionIndex  = 0;
    private float         playbackElapsedTime = 0f;
    private bool          isPlaying           = false;
    private int           currentLoop         = 0;
    private HashSet<int>  firedAtIndices      = new();
    private HashSet<int>  animatedShotIndices = new();
    private Vector3       originalScale;

    // ─── Public config API ────────────────────────────────────────────────────

    public void SetTint(Color c)                             { ghostTint = c; }
    public void SetDamageMultiplier(float m)                 { damageMultiplier = m; }
    public void SetSpread(int count, float halfAngleDeg)     { spreadCount = Mathf.Max(1, count); spreadHalfAngle = halfAngleDeg; }
    public void SetLoopCount(int loops)                      { maxLoops = Mathf.Max(1, loops); }
    public void SetPlaybackSpeed(float speed)                { playbackSpeed = Mathf.Max(0.1f, speed); }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (GetComponent<TimeParadoxDeathController>() == null)
            gameObject.AddComponent<TimeParadoxDeathController>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        rb = GetComponent<Rigidbody2D>();
    }

    public void PlayRecording(List<PlayerAction> actions)
    {
        if (actions == null || actions.Count == 0) { Destroy(gameObject); return; }

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)       animator       = GetComponentInChildren<Animator>();

        recordedActions = actions;

        if (visualsManager == null) visualsManager = FindAnyObjectByType<RecordingVisualsManager>();

        if (projectilePrefab == null)
        {
            playerShooter = FindAnyObjectByType<PlayerShooter>();
            if (playerShooter != null) projectilePrefab = playerShooter.GetProjectilePrefab();
        }
        else if (playerShooter == null)
        {
            playerShooter = FindAnyObjectByType<PlayerShooter>();
        }

        if (spriteRenderer != null) spriteRenderer.color = ghostTint;

        isPlaying           = true;
        currentLoop         = 0;
        currentActionIndex  = 0;
        playbackElapsedTime = 0f;
        firedAtIndices.Clear();
        animatedShotIndices.Clear();
        originalScale = transform.localScale;

        if (recordedActions.Count > 0)
            transform.position = recordedActions[0].position;
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isPlaying || recordedActions == null || recordedActions.Count == 0) return;

        playbackElapsedTime += Time.deltaTime * playbackSpeed;

        // Single-action edge case
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
                currentLoop++;
                if (currentLoop < maxLoops)
                {
                    EMPRingEffect.Spawn(transform.position, 2.2f, 0.38f);
                    RestartPlayback();
                }
                else
                {
                    StartCoroutine(FadeOut());
                }
            }
            return;
        }

        // Advance to the current frame
        while (currentActionIndex < recordedActions.Count - 1 &&
               playbackElapsedTime >= recordedActions[currentActionIndex + 1].timestamp)
        {
            currentActionIndex++;
        }

        // End of recording
        if (currentActionIndex >= recordedActions.Count - 1 &&
            playbackElapsedTime >= recordedActions[recordedActions.Count - 1].timestamp + 0.5f)
        {
            isPlaying = false;
            currentLoop++;
            if (currentLoop < maxLoops)
            {
                // Pulse an EMP ring where the ghost is standing, then restart
                EMPRingEffect.Spawn(transform.position, 2.2f, 0.38f);
                RestartPlayback();
            }
            else
            {
                StartCoroutine(FadeOut());
            }
            return;
        }

        // Interpolate position
        PlayerAction current = recordedActions[currentActionIndex];
        PlayerAction next    = currentActionIndex + 1 < recordedActions.Count
            ? recordedActions[currentActionIndex + 1]
            : current;

        float actionDur = next.timestamp - current.timestamp;
        if (actionDur < 0.001f) actionDur = 0.1f;

        float t = Mathf.Clamp01((playbackElapsedTime - current.timestamp) / actionDur);
        transform.position = Vector3.Lerp(current.position, next.position, t);

        ApplyAnimation(current);

        if (current.didShoot && projectilePrefab != null && !firedAtIndices.Contains(currentActionIndex))
        {
            ReplayShot(current);
            firedAtIndices.Add(currentActionIndex);
        }
    }

    private void RestartPlayback()
    {
        currentActionIndex  = 0;
        playbackElapsedTime = 0f;
        firedAtIndices.Clear();
        animatedShotIndices.Clear();
        if (recordedActions.Count > 0)
            transform.position = recordedActions[0].position;
        isPlaying = true;
    }

    // ─── Animation ────────────────────────────────────────────────────────────

    private void ApplyAnimation(PlayerAction action)
    {
        if (animator == null) return;
        animator.SetFloat("Speed",      action.movementDirection.magnitude);
        animator.SetFloat("MoveX",      action.movementDirection.x);
        animator.SetFloat("MoveY",      action.movementDirection.y);
        animator.SetBool("IsMoving",    action.movementDirection.sqrMagnitude > 0.0001f);
        animator.SetBool("IsSprinting", action.isSprinting);
        if (spriteRenderer != null && action.movementDirection.x != 0f)
            spriteRenderer.flipX = action.movementDirection.x < 0f;
    }

    // ─── Shot replay ─────────────────────────────────────────────────────────

    private void ReplayShot(PlayerAction action)
    {
        if (projectilePrefab == null) return;

        ProjectileProfile profile  = null;
        Gun               activeGun = null;
        if (playerShooter != null)
        {
            activeGun = playerShooter.GetActiveGun();
            if (activeGun != null && activeGun.CurrentProfile != null)
                profile = activeGun.CurrentProfile.ProjectileProfile;
        }

        LayerMask mask   = profile != null ? profile.HitMask : ~0;
        bool      rotate = profile != null && profile.RotateToDirection;
        float     speed  = profile != null ? profile.Speed    : 12f;
        float     life   = profile != null ? profile.Lifetime : 2f;
        int       dmg    = Mathf.RoundToInt((profile != null ? profile.Damage : 1) * damageMultiplier);

        Vector2 baseDir = action.shootDirection.sqrMagnitude > 0.001f
            ? action.shootDirection.normalized
            : Vector2.right;

        // Build the direction list:
        // spreadCount > 1 means an ability-level override (Barrage / Singularity).
        // Otherwise delegate to the gun so pellet count, spread upgrades and
        // Zarkinator poly-forms are all replicated correctly.
        List<Vector2> dirs;
        if (spreadCount > 1)
        {
            dirs = new List<Vector2>(spreadCount);
            for (int i = 0; i < spreadCount; i++)
            {
                float angleDeg = Mathf.Lerp(-spreadHalfAngle, spreadHalfAngle, (float)i / (spreadCount - 1));
                dirs.Add(RotateVector(baseDir, angleDeg));
            }
        }
        else if (activeGun != null)
        {
            dirs = activeGun.GetGhostFireDirections(baseDir);
        }
        else
        {
            dirs = new List<Vector2> { baseDir };
        }

        foreach (Vector2 dir in dirs)
        {
            Projectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            p.Initialize(dir, speed, life, dmg, gameObject, mask, rotate);
            if (profile != null) p.ApplyProfile(profile);
            p.transform.right = dir;
        }

        GameSfxManager.Instance?.PlayShoot();

        if (animator != null && !animatedShotIndices.Contains(currentActionIndex))
        {
            animator.SetTrigger("Shoot");
            animatedShotIndices.Add(currentActionIndex);
        }
    }

    private static Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ─── Fade out ─────────────────────────────────────────────────────────────

    public void StopPlayback() { isPlaying = false; Destroy(gameObject); }

    private IEnumerator FadeOut()
    {
        float   duration   = 0.4f;
        float   elapsed    = 0f;
        Color   baseColor  = spriteRenderer != null ? spriteRenderer.color : ghostTint;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t    = elapsed / duration;
            float ease = t * t;

            transform.localScale = startScale * (1f - 0.25f * ease);

            if (spriteRenderer != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
