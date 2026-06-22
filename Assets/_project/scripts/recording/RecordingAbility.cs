using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAction = PlayerActionRecorder.PlayerAction;

/// <summary>
/// Echo ability — star-aware ghost replay system.
/// ★0  Basic Echo    : cyan ghost, one-shot replay.
/// ★1  Reversal      : purple ghost plays the recording backwards.
/// ★2  Twin Echo     : blue (normal) + red (mirrored) ghost simultaneously.
/// ★3  Barrage       : gold ghost fires a 3-spread per shot.
/// ★4  Temporal Loop : green ghost loops 3× with an EMP ring on each restart.
/// ★5  Singularity   : cinematic time-slow → 5 rainbow ghosts → grand finale EMP.
/// </summary>
public sealed class RecordingAbility : Ability
{
    [SerializeField] private GhostPlayer ghostPlayerPrefab;

    private RecordingManager recordingManager;
    private bool isCurrentlyRecording = false;

    // Sprite pulse state
    private SpriteRenderer playerSprite;
    private Color          playerSpriteOriginalColor;
    private Coroutine      pulseCoroutine;

    // ─── Hotbar active-state indicator ───────────────────────────────────────
    public override bool IsInActiveState => isCurrentlyRecording;

    // ─── Ghost tints per star ─────────────────────────────────────────────────
    private static readonly Color TintCyan   = new(0.40f, 0.90f, 1.00f, 0.75f);
    private static readonly Color TintOrange = new(1.00f, 0.55f, 0.10f, 0.85f);
    private static readonly Color TintBlue   = new(0.30f, 0.50f, 1.00f, 0.75f);
    private static readonly Color TintRed    = new(1.00f, 0.30f, 0.30f, 0.70f);
    private static readonly Color TintGold   = new(1.00f, 0.82f, 0.10f, 0.80f);
    private static readonly Color TintGreen  = new(0.20f, 1.00f, 0.45f, 0.80f);

    // ─── Cooldown override ───────────────────────────────────────────────────
    // Always ready while actively recording so the player can stop at any time.
    public override bool IsReady => isCurrentlyRecording || base.IsReady;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        recordingManager = RecordingManager.Instance;
        if (recordingManager != null)
        {
            recordingManager.OnRecordingStopped += OnRecordingStoppedExternally;
            recordingManager.OnTimeExpired      += OnRecordingStoppedExternally;
        }
    }

    private void OnDestroy()
    {
        if (recordingManager != null)
        {
            recordingManager.OnRecordingStopped -= OnRecordingStoppedExternally;
            recordingManager.OnTimeExpired      -= OnRecordingStoppedExternally;
        }
    }

    private void OnDisable()
    {
        EndPlayerPulse();
        ScreenFX.Instance?.SetRecording(false);
        if (Time.timeScale < 0.5f)
            Time.timeScale = 1f;
    }

    // ─── Activation ──────────────────────────────────────────────────────────

    public override void TryActivate()
    {
        if (isCurrentlyRecording)
        {
            lastUsedTime = Time.time;
            Activate();
        }
        else if (base.IsReady)
        {
            Activate();
        }
    }

    protected override void Activate()
    {
        if (recordingManager == null) recordingManager = RecordingManager.Instance;
        if (recordingManager == null) return;

        if (isCurrentlyRecording) StopAndSpawn();
        else StartRecording();
    }

    private void StartRecording()
    {
        var device = recordingManager.GetDevice();
        if (device != null && !device.IsUnlocked) device.Unlock();
        recordingManager.StartRecording();

        // Only engage visuals/state if recording actually started
        if (!recordingManager.IsRecording) return;
        isCurrentlyRecording = true;
        ScreenFX.Instance?.SetRecording(true);
        BeginPlayerPulse();
    }

    private void StopAndSpawn()
    {
        recordingManager.StopRecording();
        isCurrentlyRecording = false;
        ScreenFX.Instance?.SetRecording(false);
        EndPlayerPulse();
        SpawnEcho();
    }

    private void OnRecordingStoppedExternally()
    {
        if (!isCurrentlyRecording) return;
        isCurrentlyRecording = false;
        ScreenFX.Instance?.SetRecording(false);
        EndPlayerPulse();
        SpawnEcho();
    }

    // ─── Player sprite pulse ──────────────────────────────────────────────────

    private void BeginPlayerPulse()
    {
        if (playerSprite == null)
        {
            var player = FindAnyObjectByType<PlayerMovement>();
            if (player != null) playerSprite = player.GetComponent<SpriteRenderer>();
        }
        if (playerSprite != null) playerSpriteOriginalColor = playerSprite.color;

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulsePlayerSprite());
    }

    private void EndPlayerPulse()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (playerSprite != null) playerSprite.color = playerSpriteOriginalColor;
    }

    private IEnumerator PulsePlayerSprite()
    {
        Color pulseColor = new Color(0.4f, 0.9f, 1f, 1f); // cyan flash
        while (true)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
            if (playerSprite != null)
                playerSprite.color = Color.Lerp(playerSpriteOriginalColor, pulseColor, t * 0.55f);
            yield return null;
        }
    }

    // ─── Echo spawning (star-aware) ───────────────────────────────────────────

    private void SpawnEcho()
    {
        if (ghostPlayerPrefab == null)
        {
            Debug.LogWarning("[RecordingAbility] ghostPlayerPrefab is not assigned in the Inspector.");
            return;
        }

        var recorder = recordingManager.GetRecorder();
        if (recorder == null) return;

        var actions = recorder.GetRecordedActions();
        if (actions.Count == 0 || recorder.RecordingDuration < 0.3f) return;

        var   player   = FindAnyObjectByType<PlayerMovement>();
        Vector3 origin = player != null ? player.GetPosition() : Vector3.zero;

        switch (GetStarLevel())
        {
            case 0:  SpawnBasic(actions, origin, player);       break;
            case 1:  SpawnPhaseClone(actions, origin, player); break;
            case 2:  SpawnTwin(actions, origin, player);        break;
            case 3:  SpawnBarrage(actions, origin, player);     break;
            case 4:  SpawnTemporalLoop(actions, origin, player);break;
            default: StartCoroutine(SingularityCinematic(actions, origin)); break;
        }
    }

    // ★0 — Basic cyan ghost
    private void SpawnBasic(List<PlayerAction> actions, Vector3 origin, PlayerMovement player)
    {
        var ghost = MakeGhost(origin);
        ghost.SetTint(TintCyan);
        LinkPartner(player, ghost);
        ghost.PlayRecording(actions);
    }

    // ★1 — Phase Clone: hot-orange ghost at 2× speed, 2× damage, double EMP burst on spawn
    private void SpawnPhaseClone(List<PlayerAction> actions, Vector3 origin, PlayerMovement player)
    {
        EMPRingEffect.Spawn(origin, 3.0f, 0.40f);
        EMPRingEffect.Spawn(origin, 1.5f, 0.28f);

        var ghost = MakeGhost(origin);
        ghost.SetTint(TintOrange);
        ghost.SetDamageMultiplier(2f);
        ghost.SetPlaybackSpeed(2f);
        LinkPartner(player, ghost);
        ghost.PlayRecording(actions);
    }

    // ★2 — Twin: blue normal + red mirrored ghost simultaneously
    private void SpawnTwin(List<PlayerAction> actions, Vector3 origin, PlayerMovement player)
    {
        var ghost1 = MakeGhost(origin);
        ghost1.SetTint(TintBlue);
        LinkPartner(player, ghost1);
        ghost1.PlayRecording(actions);

        var ghost2 = MakeGhost(origin);
        ghost2.SetTint(TintRed);
        ghost2.PlayRecording(MirrorActions(actions, origin));
    }

    // ★3 — Barrage: gold ghost fires 3-spread per shot
    private void SpawnBarrage(List<PlayerAction> actions, Vector3 origin, PlayerMovement player)
    {
        var ghost = MakeGhost(origin);
        ghost.SetTint(TintGold);
        ghost.SetSpread(3, 14f);
        LinkPartner(player, ghost);
        ghost.PlayRecording(actions);
    }

    // ★4 — Temporal Loop: green ghost loops 3× with EMP ring between loops
    private void SpawnTemporalLoop(List<PlayerAction> actions, Vector3 origin, PlayerMovement player)
    {
        var ghost = MakeGhost(origin);
        ghost.SetTint(TintGreen);
        ghost.SetLoopCount(3);
        LinkPartner(player, ghost);
        ghost.PlayRecording(actions);
    }

    // ★5 — Singularity: dramatic cinematic, 5 rainbow ghosts, grand finale EMP
    // Each colour has a distinct combat effect:
    //   RED    — 3× damage (heavy striker)
    //   GREEN  — 2× speed playback (rapid assault)
    //   BLUE   — 5-spread burst per shot (wide coverage)
    //   WHITE  — 2× damage + 3-spread (balanced powerhouse)
    //   YELLOW — 1.5× damage + 2× speed (blitz runner)
    private IEnumerator SingularityCinematic(List<PlayerAction> actions, Vector3 center)
    {
        // ── Act I: Dramatic time freeze ─────────────────────────────────────
        Time.timeScale = 0.15f;
        EMPRingEffect.Spawn(center, 5.5f, 0.70f);
        EMPRingEffect.Spawn(center, 2.5f, 0.45f);

        yield return new WaitForSecondsRealtime(0.55f);

        Time.timeScale = 1f;

        // ── Act II: 5 rainbow ghosts fan out from centre ─────────────────────
        Color[] rainbow =
        {
            new Color(1.00f, 0.00f, 0.00f, 0.88f), // RED
            new Color(0.00f, 1.00f, 0.00f, 0.88f), // GREEN
            new Color(0.00f, 0.35f, 1.00f, 0.88f), // BLUE
            new Color(1.00f, 1.00f, 1.00f, 0.88f), // WHITE
            new Color(1.00f, 0.90f, 0.00f, 0.88f), // YELLOW
        };

        const float spawnRadius = 1.3f;

        for (int i = 0; i < 5; i++)
        {
            float   angle  = (i * 72f - 90f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            var ghost = MakeGhost(center + offset);
            ghost.SetTint(rainbow[i]);

            switch (i)
            {
                case 0: // RED — 3× damage, slower
                    ghost.SetDamageMultiplier(3f);
                    ghost.SetPlaybackSpeed(0.8f);
                    break;
                case 1: // GREEN — rapid assault, 2× speed
                    ghost.SetDamageMultiplier(1.2f);
                    ghost.SetPlaybackSpeed(2f);
                    break;
                case 2: // BLUE — wide 5-spread burst
                    ghost.SetSpread(5, 20f);
                    ghost.SetDamageMultiplier(1.3f);
                    break;
                case 3: // WHITE — balanced powerhouse
                    ghost.SetDamageMultiplier(2f);
                    ghost.SetSpread(3, 15f);
                    break;
                case 4: // YELLOW — blitz runner
                    ghost.SetDamageMultiplier(1.5f);
                    ghost.SetPlaybackSpeed(2f);
                    ghost.SetSpread(3, 10f);
                    break;
            }

            ghost.PlayRecording(actions);
            yield return new WaitForSeconds(0.22f);
        }

        // ── Act III: Grand finale after all ghosts finish ────────────────────
        float recordDuration = actions.Count > 0 ? actions[actions.Count - 1].timestamp : 2f;
        yield return new WaitForSeconds(recordDuration + 1.5f);

        EMPRingEffect.Spawn(center, 12f, 1.10f);
        EMPRingEffect.Spawn(center,  8f, 0.80f);
        EMPRingEffect.Spawn(center,  4f, 0.55f);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private GhostPlayer MakeGhost(Vector3 position)
        => Instantiate(ghostPlayerPrefab, position, Quaternion.identity);

    private static void LinkPartner(PlayerMovement player, GhostPlayer ghost)
    {
        if (player == null) return;
        var pd = player.GetComponent<TimeParadoxDeathController>();
        var gd = ghost.GetComponent<TimeParadoxDeathController>();
        if (pd != null && gd != null) { pd.SetPartner(gd); gd.SetPartner(pd); }
    }

    // Mirror all positions horizontally around the centre point.
    private static List<PlayerAction> MirrorActions(List<PlayerAction> src, Vector3 center)
    {
        var result = new List<PlayerAction>(src.Count);
        foreach (PlayerAction a in src)
        {
            result.Add(new PlayerAction
            {
                timestamp         = a.timestamp,
                position          = new Vector2(2f * center.x - a.position.x, a.position.y),
                movementDirection = new Vector2(-a.movementDirection.x, a.movementDirection.y),
                isSprinting       = a.isSprinting,
                didShoot          = a.didShoot,
                shootDirection    = new Vector2(-a.shootDirection.x, a.shootDirection.y),
            });
        }
        return result;
    }
}
